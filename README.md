# CvSU AIS — ASP.NET Core rebuild

A ground-up rebuild of the Cavite State University **Accounting Information
System** (Philippine government fund accounting) on **ASP.NET Core 10 + EF Core +
PostgreSQL**, migrating off a Frappe/ERPNext implementation.

The original is a working, COA-audit-traced Frappe app. This rebuild is a
**portfolio project**: it keeps the hard, interesting parts — a provably-correct
double-ledger core and a single, role-gated disbursement workflow — and rebuilds
them with the domain modeled the way government fund accounting actually works.

> **Status:** Phase 1 walking skeleton. The domain core and its executable
> invariant suite are complete and green (**45/45**). Infrastructure (EF Core +
> Postgres) and the HTTP API are next — see the roadmap.

---

## Why a rebuild, and what it fixes

A review of the Frappe app surfaced three defects rooted in framework
impedance. Rather than patch them, this rebuild makes each one *structurally
impossible*:

| Defect in the Frappe app | How the model eliminates it |
|---|---|
| **Dual state machine + role bypass.** Both the Frappe Workflow engine and the Python controller drove `workflow_status`, and the whitelisted `set_workflow_status` saved with `ignore_permissions`, leaving a role-free path to advance a DV. | **One** explicit transition table `(from, to, action, required_role, guard)`. Firing a transition is the *only* way state changes, and every edge is role-gated server-side. There is no second engine to drift from, and no bypass. See [`DvWorkflow.cs`](src/CvSU.Ais.Domain/Disbursement/DvWorkflow.cs). |
| **Fund-dimension denormalization.** The DV stored both `funding_source` and a free-standing `fund_cluster`, derived from a different field and never cross-validated — so they could silently diverge. | The fund cluster is reachable **only** through `FundingSource.Cluster`. There is no independently-settable cluster field, so divergence is unrepresentable. See [`FundCluster.cs`](src/CvSU.Ais.Domain/Funds/FundCluster.cs) / [`FundingSource.cs`](src/CvSU.Ais.Domain/Funds/FundingSource.cs). |
| **GL-posting-point ambiguity.** Docs said one thing, code posted at another stage. | Two named stages: `Post` records the **accrual** GL entry; `Release` records the **cash** disbursement + budget-ledger Disbursement entry. The workflow documents itself. |

## The two books (this is the crux of government accounting)

The system keeps two parallel ledgers and never conflates them:

- **Accrual general ledger** (`GeneralLedgerEntry`) — PPSAS accrual basis. Each
  line is strictly **debit XOR credit**; a journal must **balance** (R-GL-01);
  posted lines are **immutable** (corrections are reversing entries).
- **Budget registry** (`BudgetLedgerEntry`) — cash-basis memorandum book that
  tracks the execution cycle **Appropriation → Allotment → Obligation →
  Disbursement**. An obligation is a memorandum entry here and **never** touches
  the accrual GL. Each entry's debit/credit side is *derived from its entry type*
  (CLAUDE.md §4A.7), so it cannot be posted to the wrong side.

Ceilings cascade and are enforced in the aggregates: allotment ≤ appropriation
(R-BUD-01), obligation ≤ allotment (R-BUD-02), STF (fund 05) cannot fund
Personnel Services (R-BUD-05), and a single obligation cannot mix fund clusters.

## Architecture

Clean Architecture, dependencies pointing inward:

```
src/
  CvSU.Ais.Domain          ← entities, value objects, aggregates, invariants (no deps)
  CvSU.Ais.Application      ← use cases / commands, ports (depends on Domain)
  CvSU.Ais.Infrastructure   ← EF Core, Postgres, interceptors (depends on Application)
  CvSU.Ais.Api              ← ASP.NET Core Web API, auth policies (depends on Infrastructure)
tests/
  CvSU.Ais.Domain.Tests     ← the executable invariant contract (xUnit)
```

The **Domain** project is pure C# with zero infrastructure dependencies, so the
invariant suite runs in milliseconds and is the acceptance gate for every later
layer.

## The invariant suite

The tests are the executable specification — the same contract the Frappe app
enforced in Python, now stack-agnostic and fast:

- Fund dimension is derived once; registry type and the STF-PS rule follow the cluster.
- UACS completeness — a half-specified budget line is unrepresentable.
- GL lines are debit-XOR-credit; journals balance to ±₱0.01; budget entries post to their canonical side.
- Budget ceilings (R-BUD-01/02), STF-PS prohibition (R-BUD-05), no cross-cluster contamination.
- DV state machine: illegal edges rejected, **every transition role-gated**, SoD enforced against direct invocation, certification gates, Administrator escape hatch, and a **drift test** asserting the transition set matches the intended workflow.

## Running it

Prerequisites: **.NET SDK 10**.

```bash
dotnet test          # run the invariant suite (45 tests)
dotnet build         # build the whole solution
```

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Domain ledger core + DV state machine + invariant suite | ✅ done |
| — | Infrastructure: EF Core + Postgres, CHECK constraints, immutability `SaveChanges` interceptor, gapless voucher-number service, `FOR UPDATE`/`SERIALIZABLE` ceiling enforcement | ⏳ next |
| — | API: per-transition ASP.NET Core authorization policies, command handlers, `Program.cs` DI; Docker Compose (api + postgres) | ⏳ |
| — | Reports (RAOD/RAPAL/trial balance) + official DV print/PDF | ⏳ |

The production-migration concerns from the full plan (shadow-reconcile against
live Frappe, ETL of audit-traced data, COA cutover) are intentionally **out of
scope** for the portfolio build — this repo showcases the architecture and the
correctness core, not a live cutover.

## License

MIT.
