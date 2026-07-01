# CvSU AIS — Business Logic Hardening Backlog

_Branch: `feat/business-logic-implementation` • Created: 2026-07-02_

Grounded in an adversarially-verified review of the branch (36 confirmed findings:
4 Critical, 15 High, 11 Medium, 6 Low). Each item cites the finding it closes.
Estimated at ~17 working days; sequenced Critical → High → Medium → Low.

> **STATUS (2026-07-02): 35 of 36 findings fixed this session** — all 4 Critical, all 15 High,
> all 11 Medium, and 5 of 6 Low. The only deferred finding is **F36** (optimistic concurrency
> tokens — needs a schema migration). See **IMPROVEMENT_PLAN.md** for what shipped, what was
> deferred and why, and the forward roadmap. Solution builds clean (0 errors); domain tests 45/45;
> new DB-free `Application.Tests` cover the Critical/High guards.

Legend: 🔴 Critical 🟠 High 🟡 Medium 🟢 Low • `[Fnn]` = confirmed finding id.

---

## Sprint 1 — Stop the money bleeding (Critical) · Days 1–3

- [ ] **D1 · 🔴 Export create crash** `[F3]` — `ExportService` name generation slices
  `[..Math.Min(140,50)]` on 45–48-char strings → `ArgumentOutOfRangeException` on **every**
  FinDES/Bank export create. Slice defensively / drop the slice.
- [ ] **D1 · 🔴 Journal Entry double-post** `[F2]` — `PostAsync` guards only on
  `ApprovalStatus=="Approved"`, never on `Posted`/`GlPostingReference`; `ApproveAsync`/`CancelAsync`
  have no from-state guard, so a Posted JE can be re-approved and re-posted → duplicate GL batch.
  Make `PostAsync` idempotent + lock the JE state machine.
- [ ] **D2 · 🔴 ORS/BURS double-obligation** `[F1]` — no state guards on Submit/Review/Sign/Cancel;
  a FundVerified ORS can be reverted to Reviewed and re-fund-verified → allotment obligated twice.
  Add an allowed-transition table (mirror `DvStateMachine`) + idempotent obligate.
- [ ] **D3 · 🔴 Regular payroll cannot post** `[F4]` — `TotalGrossPay/NetPay/Gsis/…` hardcoded `0`
  at create and never computed → `PostAsync` always fails. Add a compute/import step
  (analogous to `JoCosPayrollService.ComputeAsync`) + input surface + persist method.

## Sprint 2 — Authorization on every money-moving endpoint (High) · Days 4–5

- [ ] **D4 · 🟠 Policy scaffolding** — add `JePolicies`, `Budget/ObligationPolicies`,
  `CompliancePolicies`, `PayrollPolicies`, `CashAdvancePolicies`, `LddapPolicies`,
  `DvTransmittalPolicies` mirroring `DvPolicies/DvRoles`; register in `Program.cs`.
- [ ] **D4 · 🟠 Apply role gates** `[F5,F7,F8,F9,F12,F19,F24]` — replace bare `[Authorize]` on
  JE approve/post/cancel, ORS fund-verify, LDDAP-ADA approve/transmit, WHT/BIR2307/COA,
  CashAdvance/Payroll/Liquidation/SalaryTranche, and NCA create with per-action policies.
- [ ] **D5 · Verify** — negative tests: wrong-role principal → 403 on each gated action.

## Sprint 3 — State-machine guards everywhere else (High/Med) · Day 6

- [ ] **D6 · 🟠 FinDES export transitions** `[F11]` — guard Export/Approve/Reject by from-state.
- [ ] **D6 · 🟠 WHT statement approve/reject** `[F14]` — reject if not Draft/ForReview.
- [ ] **D6 · 🟡 BIR2307 approve/reject** `[F27]`, **COA case** `[F28]`, **Payments** `[F20]` —
  encode allowed from-states per transition.

## Sprint 4 — Concurrency-safe document numbering (High/Med) · Days 7–8

- [ ] **D7 · 🟠 ORS/BURS numbering** `[F6,F15]` — replace hand-rolled `VoucherCounter++` with
  `IVoucherNumberGenerator` (advisory-lock, gapless) inside a transaction.
- [ ] **D7 · 🟠 LDDAP/DV-Transmittal/Audit-Intake numbering** `[F16]` — same treatment.
- [ ] **D8 · 🟠 Collections OP/OR/RCD numbering** `[F17]` + **date-vs-count mismatch** `[F26]` —
  base name on the document's own business date; use the gapless generator.

## Sprint 5 — Input & amount validation (Med/Low) · Day 9

- [ ] **D9 · 🟡 Positive/zero/line-sum validation** `[F21,F22,F29,F30]` — LDDAP items,
  ORS amount vs lines, BIR2307/WHT/COA non-negative, cash-advance amount positive.
- [ ] **D9 · 🟢 Reference-data duplicate key** `[F34]` — existence check → 409 instead of raw 500.

## Sprint 6 — Reconciliation correctness (High/Med) · Days 10–12

- [ ] **D10 · 🟠 OR vs Order-of-Payment** `[F10]` — load OP, require Issued, validate/settle amount.
- [ ] **D10 · 🟠 WHT line tax recompute** `[F13]` — recompute each line `Rate*Base`, reconcile header.
- [ ] **D11 · 🟠 Cash-advance double-liquidation** `[F18]` — enforce single settlement against advance.
- [ ] **D12 · 🟡 FinDES variance** `[F25]` — compute DV totals / export totals / variance for real.
- [ ] **D12 · 🟢 WHT post route + GL ref** `[F35]` — expose policy-gated post; stamp real GL id.

## Sprint 7 — Auth, config & data-integrity hardening (Med/Low) · Days 13–15

- [ ] **D13 · 🟡 Frappe introspection audience** `[F23]` — validate `client_id/aud` (and `exp`).
- [ ] **D14 · 🟢 CORS gating** `[F32]`, **migrations-on-startup gating** `[F31]`,
  **secrets out of source** `[F33]`, **bump `Microsoft.OpenApi`** (known high-sev advisory).
- [ ] **D15 · 🟢 Optimistic concurrency tokens** `[F36]` — `xmin` rowversion on status-bearing
  entities (DV, JE, CashAdvance, Liquidation, …) + EF migration.

## Sprint 8 — Tests, verification & PR · Days 16–17

- [ ] **D16 · Regression tests** — one test per Critical/High fix (double-post, double-obligation,
  double-liquidation, export crash, payroll compute, authz 403s, numbering races).
- [ ] **D17 · Green build + full test pass**, update handoff docs, open PR.

---

### Themes (root causes)
1. **Idempotency / state machines** — `UpdateStatusAsync` writes status with no from-state guard
   across many modules; posting methods aren't idempotent (`[F1,F2,F11,F14,F18,F20,F27,F28]`).
2. **Authorization** — controllers ship bare `[Authorize]`; no role/SoD on money movement
   (`[F5,F7,F8,F9,F12,F19,F24]`).
3. **Numbering races** — bespoke counter logic instead of the gapless advisory-lock generator
   (`[F6,F15,F16,F17,F26]`).
4. **Validation** — amounts/lines not checked before persist (`[F10,F13,F21,F22,F29,F30,F34]`).
5. **Incomplete features** — payroll compute, FinDES variance, WHT post route (`[F4,F25,F35]`).
6. **Hardening** — auth audience, CORS, migrations, secrets, concurrency tokens (`[F23,F31,F32,F33,F36]`).
