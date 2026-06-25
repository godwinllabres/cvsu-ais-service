# Postman — CvSU AIS API

Import these two files into Postman:

- `CvSU-AIS-API.postman_collection.json` — all endpoints, grouped by area.
- `CvSU-AIS-API.local.postman_environment.json` — `baseUrl` + the per-role test users.

Select the **CvSU AIS - Local** environment in the top-right, then either fire
requests individually or use **Collection Runner** on a folder.

## Start the API first

```powershell
& .claude\skills\run-cvsu-ais-api\driver.ps1
```

Defaults to `http://localhost:5186` (matches `baseUrl`).

## Auth model

There is no login. Every request carries two dev headers:

- `X-User` — the acting user.
- `X-Roles` — comma-separated roles.

No headers → **401**; authenticated but wrong role → **403**. The collection sets
both headers per request to the role each endpoint requires.

| Role header value   | Can do |
|---------------------|--------|
| `Accounting Clerk`  | Create DV, request IA audit |
| `Internal Auditor`  | Submit, return to clerk |
| `Accountant`        | Approve, post, close, view reports |
| `Head of Agency`    | Approve for payment, view reports |
| `Cashier`           | Release |
| `Budget Officer`    | Appropriations, allotments, obligations, view reports |

## Folders

- **DV Lifecycle (happy path)** — run top-to-bottom. `Create DV` stores the
  generated name in `{{dvName}}`; every later step reuses it. Each transition
  uses a **different** `X-User` because Segregation of Duties forbids the
  encoder, approver, and payment-approver from overlapping.
- **Budget Execution** — Appropriation → Allotment → Obligation. Captures
  `{{appropriationId}}` and `{{allotmentId}}` along the way.
- **Reports** — RAOD/RBUD budget registry, RAPAL, trial balance (current FY).
- **Auth & alternate flows** — the `return-to-clerk` branch plus 401/403 checks.

## Notes

- Seeded funding sources: `01101101` (Regular Agency Fund, cluster 01/RAOD) and
  `05101101` (Internally Generated Funds/STF, cluster 05/RBUD). Other codes 404.
- `expenseClass` is one of `Ps`, `Mooe`, `Fe`, `Co` (sent as a JSON string).
- The DV `Create` body sets all certifications + the accountant signature to
  `true` so the voucher can traverse the whole workflow; drop any of them to
  see the certification/SoD guards reject `approve`/`post`.
- Domain rule violations come back as RFC 7807 `application/problem+json` with a
  descriptive message and the rule id (e.g. `R-DV-09` for SoD).
