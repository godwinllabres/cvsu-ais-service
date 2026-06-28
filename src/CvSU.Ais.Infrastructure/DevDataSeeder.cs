using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CvSU.Ais.Infrastructure;

/// <summary>
/// Seeds a small, balanced set of accrual GL entries (and budget-ledger entries) for the
/// current fiscal year so the financial statements and registries render real figures in
/// development. Idempotent: it only seeds when the general ledger is empty, so it never
/// duplicates on restart and never touches a database that already has posted activity.
///
/// The accounts follow the Revised Chart of Accounts (RCA) major groups by leading digit —
/// 1 Assets · 2 Liabilities · 3 Equity · 4 Revenue · 5 Expenses — which is what the financial
/// statements classify on. Every journal balances (debits = credits).
/// </summary>
public sealed class DevDataSeeder(AisDbContext db, ILogger<DevDataSeeder> logger)
{
    /// <summary>The marker voucher of the opening journal — its presence means we have already
    /// seeded, so we never duplicate (idempotent even alongside real DV/GL activity).</summary>
    private const string MarkerVoucher = "JEV-OPEN-001";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var fy = DateTime.UtcNow.Year;
        var date = new DateOnly(fy, 1, 31);

        var glAlreadySeeded = await db.GeneralLedger.AnyAsync(e => e.VoucherNo == MarkerVoucher, cancellationToken);
        var budgetAlreadySeeded = await db.BudgetLedger.AnyAsync(e => e.VoucherNo == BudgetMarker, cancellationToken);
        if (glAlreadySeeded && budgetAlreadySeeded)
            return; // both already seeded — nothing to do.

        if (!glAlreadySeeded)
            SeedGl(fy, date);
        if (!budgetAlreadySeeded)
            SeedBudget(fy, date);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded GL={GlNew} Budget={BudgetNew} for FY {Year} (GL total {Gl}, Budget total {Bud}).",
            !glAlreadySeeded, !budgetAlreadySeeded, fy,
            await db.GeneralLedger.CountAsync(cancellationToken),
            await db.BudgetLedger.CountAsync(cancellationToken));
    }

    private void SeedGl(int fy, DateOnly date)
    {
        logger.LogInformation("Seeding development GL data for FY {Year}.", fy);

        // A coherent illustrative set: opening cash & equity, recognised income (subsidy + tuition),
        // and operating expenses settled in cash. Each tuple is one balanced journal.
        // Account codes are RCA-style (1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense).
        void Post(string account, decimal debit, decimal credit, string doctype, string voucher, string remarks)
            => db.GeneralLedger.Add(new GeneralLedgerRow
            {
                PostingDate = date,
                FiscalYear = fy,
                Account = account,
                Debit = debit,
                Credit = credit,
                VoucherDoctype = doctype,
                VoucherNo = voucher,
                Remarks = remarks,
            });

        // 1) Opening balances: Cash in Bank (asset) against Accumulated Surplus (equity).
        Post("10101010", 12_000_000m, 0m, "JEV", "JEV-OPEN-001", "Opening Cash in Bank–LCCA");
        Post("30101010", 0m, 12_000_000m, "JEV", "JEV-OPEN-001", "Accumulated Surplus/(Deficit), beginning");

        // 2) Receivable set up for an unreleased subsidy, against equity (illustrative asset/equity).
        Post("10301010", 1_500_000m, 0m, "JEV", "JEV-OPEN-002", "Accounts Receivable");
        Post("30101010", 0m, 1_500_000m, "JEV", "JEV-OPEN-002", "Accumulated Surplus/(Deficit), beginning");

        // 3) Revenue — Assistance/Subsidy from National Government (collected in cash).
        Post("10101010", 8_000_000m, 0m, "JEV", "JEV-REV-001", "Cash receipt — NG subsidy");
        Post("40301010", 0m, 8_000_000m, "JEV", "JEV-REV-001", "Subsidy from National Government");

        // 4) Revenue — Tuition and other school fees (collected in cash).
        Post("10101010", 3_200_000m, 0m, "JEV", "JEV-REV-002", "Cash receipt — tuition & fees");
        Post("40202010", 0m, 3_200_000m, "JEV", "JEV-REV-002", "School Fees");

        // 5) Expense — Salaries and Wages (PS), paid in cash.
        Post("50101010", 6_400_000m, 0m, "DV", "DV-2026-50001", "Salaries and Wages — Regular");
        Post("10101010", 0m, 6_400_000m, "DV", "DV-2026-50001", "Payment of salaries");

        // 6) Expense — Office Supplies (MOOE), paid in cash.
        Post("50203010", 1_100_000m, 0m, "DV", "DV-2026-50002", "Office Supplies Expenses");
        Post("10101010", 0m, 1_100_000m, "DV", "DV-2026-50002", "Payment of supplies");

        // 7) Expense — Electricity (MOOE), set up as a payable (liability) not yet paid.
        Post("50204020", 450_000m, 0m, "JEV", "JEV-EXP-003", "Electricity Expenses");
        Post("20101010", 0m, 450_000m, "JEV", "JEV-EXP-003", "Accounts Payable — utilities");

        // 8) Additional GL expenses so the FS span every allotment class:
        //    Capital Outlay (CO) — equipment purchased, paid in cash.
        Post("50604050", 2_800_000m, 0m, "DV", "DV-2026-50003", "Office Equipment (Capital Outlay)");
        Post("10101010", 0m, 2_800_000m, "DV", "DV-2026-50003", "Payment for equipment");
        //    Financial Expenses (FE) — bank charges.
        Post("50301020", 35_000m, 0m, "DV", "DV-2026-50004", "Bank Charges (Financial Expenses)");
        Post("10101010", 0m, 35_000m, "DV", "DV-2026-50004", "Payment of bank charges");
    }

    private const string BudgetMarker = "SEED-BUDGET-001";

    // ── Budget ledger ──────────────────────────────────────────────────────────────────────
    // The registries (RAOD/RBUD/RAPAL/SAAODB/SCBAA) and FAR No. 4 read the BUDGET ledger, not
    // the GL — so seed a full appropriation → allotment → obligation → disbursement cycle for
    // every allotment class, on both fund clusters, so those reports show real figures across
    // PS/MOOE/FinEx/CO rather than zeros.

    private void SeedBudget(int fy, DateOnly date)
    {
        void PostBudget(string fundingSource, ExpenseClass cls, string objectCode,
            BudgetEntryType type, decimal amount, string voucher, string? apprId = null, string? allotId = null)
        {
            var side = EntryTypeSide.For(type);
            db.BudgetLedger.Add(new BudgetLedgerRow
            {
                PostingDate = date,
                FiscalYear = fy,
                FundingSourceCode = fundingSource,
                PapCode = "100000100001000",
                LocationCode = "0456",
                ExpenseClass = cls,
                ObjectAccountCode = objectCode,
                EntryType = type,
                Debit = side == LedgerSide.Debit ? amount : 0m,
                Credit = side == LedgerSide.Credit ? amount : 0m,
                VoucherDoctype = "BUDGET",
                VoucherNo = voucher,
                AppropriationId = apprId,
                AllotmentId = allotId,
            });
        }

        // One full cycle per allotment class on the Regular Agency Fund (cluster 01 → RAOD/RAPAL).
        // amounts: (appropriation, allotment, obligation, disbursement) — each ≤ the prior ceiling.
        (ExpenseClass cls, string obj, decimal app, decimal allot, decimal oblig, decimal disb)[] regular =
        [
            (ExpenseClass.Ps,   "5010101000", 40_000_000m, 38_000_000m, 35_000_000m, 32_000_000m),
            (ExpenseClass.Mooe, "5020399000", 18_000_000m, 16_500_000m, 14_000_000m, 11_500_000m),
            (ExpenseClass.Fe,   "5030102000",    600_000m,    550_000m,    420_000m,    380_000m),
            (ExpenseClass.Co,   "5060405000", 12_000_000m, 10_000_000m,  8_500_000m,  6_200_000m),
        ];

        var n = 1;
        foreach (var r in regular)
        {
            var app = $"APP-{fy}-SEED{n:00}";
            var allot = $"ALL-{fy}-SEED{n:00}";
            PostBudget("01101101", r.cls, r.obj, BudgetEntryType.Appropriation, r.app, $"{BudgetMarker}", app);
            PostBudget("01101101", r.cls, r.obj, BudgetEntryType.Allotment, r.allot, $"SEED-ALLOT-{n:00}", app, allot);
            PostBudget("01101101", r.cls, r.obj, BudgetEntryType.Obligation, r.oblig, $"SEED-OBR-{n:00}", app, allot);
            PostBudget("01101101", r.cls, r.obj, BudgetEntryType.Disbursement, r.disb, $"SEED-DISB-{n:00}", app, allot);
            n++;
        }

        // A smaller cycle on the Internally Generated Fund (cluster 05 → RBUD), MOOE + CO.
        (ExpenseClass cls, string obj, decimal app, decimal allot, decimal oblig, decimal disb)[] igf =
        [
            (ExpenseClass.Mooe, "5020399000", 6_000_000m, 5_200_000m, 4_100_000m, 3_300_000m),
            (ExpenseClass.Co,   "5060405000", 3_000_000m, 2_400_000m, 1_900_000m, 1_400_000m),
        ];
        foreach (var r in igf)
        {
            var app = $"APP-{fy}-SEED{n:00}";
            var allot = $"ALL-{fy}-SEED{n:00}";
            PostBudget("05101101", r.cls, r.obj, BudgetEntryType.Appropriation, r.app, $"SEED-APP-{n:00}", app);
            PostBudget("05101101", r.cls, r.obj, BudgetEntryType.Allotment, r.allot, $"SEED-ALLOT-{n:00}", app, allot);
            PostBudget("05101101", r.cls, r.obj, BudgetEntryType.Obligation, r.oblig, $"SEED-OBR-{n:00}", app, allot);
            PostBudget("05101101", r.cls, r.obj, BudgetEntryType.Disbursement, r.disb, $"SEED-DISB-{n:00}", app, allot);
            n++;
        }
    }
}
