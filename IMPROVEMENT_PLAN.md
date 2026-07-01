# CvSU AIS — Improvement Plan (post-hardening)

_Branch: `feat/business-logic-implementation` • 2026-07-02_

This session took an adversarially-verified review of the branch (36 confirmed findings)
and **fixed 35 of 36** plus the security/config hardening. This document records what
shipped, what was deliberately deferred (with rationale), and the forward roadmap.

## What shipped this session

**Correctness / accounting integrity**
- JE double-post closed — `PostAsync` is idempotent and the JE state machine is locked (F2).
- ORS/BURS backward-transition + double-obligation closed — explicit allowed-transition table;
  fund-verify can't run twice (F1, F22).
- Regular payroll can now post — create accepts the register summary; net is computed; posting
  rejects unpopulated payroll (F4).
- Cash-advance double-liquidation closed — single settlement enforced against the advance (F18, F30).
- FinDES / Bank export create no longer crashes on every call (F3); FinDES variance is computed
  for real (F25).
- State-machine guards added across Exports, WHT, BIR 2307, COA, LDDAP-ADA, DV Transmittal,
  Audit Intake (F11, F14, F20, F27, F28).
- WHT line tax recomputed from Rate×Base and reconciled to the header (F13).
- Input/amount validation across obligations, payroll, cash advances, compliance, payments,
  reference data (F21, F29, F34).

**Concurrency-safe numbering**
- ORS/BURS, LDDAP-ADA, DV-Transmittal, Audit-Intake, and Collections (OP/OR/RCD) numbering now
  use the gapless advisory-lock generator inside a transaction, replacing racy
  `counter++` / `CountAsync+1` logic (F6, F15, F16, F17, F26).

**Authorization (defense-in-depth, mirroring the DV module)**
- 8 new policy classes; every money-moving endpoint across 20 controllers now carries a
  per-action role policy instead of a bare `[Authorize]` (F5, F7, F8, F9, F12, F19, F24).
- New WHT "post to GL" route, policy-gated (F35).

**Security / config hardening**
- Frappe introspection now enforces token audience/`client_id` and rejects expired tokens (F23).
- CORS any-origin gated to Development; explicit allow-list elsewhere (F32).
- Auto-migrate on startup gated to Development / explicit opt-in (F31).
- DB credentials removed from committed `appsettings.json`; fail-fast outside Development (F33).

## Deferred — and why

| Item | Finding | Why deferred | Recommended action |
|---|---|---|---|
| **Optimistic concurrency tokens** on status-bearing entities | F36 | Needs an EF schema migration (`xmin` rowversion) + `DbUpdateConcurrencyException` handling across every `UpdateStatusAsync`. The new from-state guards already mitigate the double-transition race at the app layer. | Add `xmin` concurrency token to DV/JE/CashAdvance/Liquidation/ORS rows + migration; handle the concurrency exception as a 409. |
| **Payroll register line-level import** | extends F4 | This session made regular payroll postable via summary totals (no schema change). Full per-employee register lines need a new table + migration + import parser. | Add `payroll_register_line` table, CSV/XLSX import, per-employee GL detail. |
| **`Microsoft.OpenApi` advisory** GHSA-v5pm-xwqc-g5wc | — | The whole 2.x line is flagged; the fix is 3.x, which is API-incompatible with `Microsoft.AspNetCore.OpenApi 10.0.9`. This API only *produces* OpenAPI docs (never parses untrusted ones), so exploitability is minimal. | Track for an upstream ASP.NET Core patch referencing a fixed `Microsoft.OpenApi`, or validate a 3.x migration behind runtime tests. |
| **HTTP-level authz negative tests** (403 for wrong role) | Sprint 2 D5 | Needs the WebApplicationFactory + Postgres integration harness; **Docker was unavailable** in this environment. | Run in CI with Docker; assert 403 per gated endpoint. |
| **Integration test run** | — | Testcontainers Postgres needs Docker (down here). New Application-layer unit tests run DB-free. | Run `Integration.Tests` + new `Application.Tests` in CI. Fix the `PostgreSqlBuilder` `CS0618` obsolete-ctor warning while there. |

## Pre-existing items surfaced (not introduced by this branch)
- **DV post/release lacks a row lock / concurrency token** (the budget path uses `FOR UPDATE`; the
  DV posting path does not) — two concurrent posts could double-post the GL. Ties to F36.
- **Ledger immutability is interceptor-only** — `ExecuteUpdate/ExecuteDelete` and non-EF access
  bypass it. Add a migration `REVOKE UPDATE, DELETE ON gl_entry, budget_ledger_entry` (or triggers).

## Forward roadmap (beyond the 17-day backlog)
1. Concurrency tokens + the two pre-existing ledger-integrity items above.
2. Payroll register line import; FinDES/bank export end-to-end with real DV reconciliation data.
3. Confirm the RBAC role→action matrix with a CvSU domain owner (policies are documented inline).
4. CI: run the full test matrix (unit + integration) on Docker; wire the security advisory scan.
5. Perf pass on the reporting queries once real data volume exists.
