using CvSU.Ais.Application.Budget;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Application.Reports;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>The COA registries computed off the ledgers against real Postgres.
/// Each test posts into a unique fiscal year so the shared container's
/// accumulated rows never bleed into the assertions.</summary>
[Collection("postgres")]
public class ReportingTests(PostgresFixture fixture)
{
    private static int _yearSeed = 4000;
    private static int NextYear() => Interlocked.Increment(ref _yearSeed);

    private static BudgetExecutionService Budget(AisDbContext db)
    {
        var catalog = new FundingSourceCatalog(db);
        return new BudgetExecutionService(
            new BudgetLedgerRepository(db, catalog), catalog,
            new GaplessVoucherNumberService(db), new UnitOfWork(db));
    }

    private static DisbursementVoucherService Dv(AisDbContext db)
    {
        var catalog = new FundingSourceCatalog(db);
        return new DisbursementVoucherService(
            new DisbursementVoucherRepository(db, catalog), catalog,
            new GaplessVoucherNumberService(db),
            new GeneralLedgerRepository(db), new BudgetLedgerRepository(db, catalog),
            new UnitOfWork(db));
    }

    private static ReportingService Reports(AisDbContext db) => new(new ReportingQueries(db));

    [Fact]
    public async Task Rapal_reflects_appropriation_and_allotment_for_the_year()
    {
        var year = NextYear();
        await using var db = fixture.CreateContext();
        var budget = Budget(db);

        var appropriation = await budget.CreateAppropriationAsync(new CreateAppropriationCommand(
            year, "01101101", "PAP-A", "LOC-A", ExpenseClass.Mooe, "50203010", 1_000_000m));
        await budget.AllotAsync(appropriation.Id, 600_000m);

        var report = await Reports(db).AppropriationAllotmentAsync(year);

        var line = Assert.Single(report.Lines);
        Assert.Equal("01", line.FundClusterCode);
        Assert.Equal(ExpenseClass.Mooe, line.ExpenseClass);
        Assert.Equal(1_000_000m, line.Appropriation);
        Assert.Equal(600_000m, line.Allotment);
        Assert.Equal(400_000m, line.UnallottedBalance);
        Assert.Equal(400_000m, report.TotalUnallotted);
    }

    [Fact]
    public async Task Raod_reflects_allotment_and_obligation_for_a_raod_cluster()
    {
        var year = NextYear();
        await using var db = fixture.CreateContext();
        var budget = Budget(db);

        var appropriation = await budget.CreateAppropriationAsync(new CreateAppropriationCommand(
            year, "01101101", "PAP-A", "LOC-A", ExpenseClass.Mooe, "50203010", 1_000_000m));
        var allotment = await budget.AllotAsync(appropriation.Id, 600_000m);
        await budget.ObligateAsync(allotment.Id, 200_000m);

        var report = await Reports(db).BudgetRegistryAsync(year);

        // Cluster 01 reports through RAOD, not RBUD.
        Assert.Empty(report.Rbud.Lines);
        var line = Assert.Single(report.Raod.Lines);
        Assert.Equal("01", line.FundClusterCode);
        Assert.Equal(600_000m, line.Allotment);
        Assert.Equal(200_000m, line.Obligation);
        Assert.Equal(400_000m, line.UnobligatedBalance);
        Assert.Equal(200_000m, line.UnpaidObligation); // obligated, none yet disbursed
        Assert.Equal(600_000m, report.Raod.Totals.Allotment);
    }

    [Fact]
    public async Task Released_dv_posts_a_budget_disbursement_into_its_posting_year_registry()
    {
        await using var db = fixture.CreateContext();
        var dvs = Dv(db);

        // The DV's budget Disbursement entry uses the posting-date year (current year),
        // so we assert it CONTRIBUTES to that year's RAOD disbursement total rather than
        // owning an isolated line (the shared container may hold other current-year rows).
        var postingYear = DateTime.UtcNow.Year;
        var before = await Reports(db).BudgetRegistryAsync(postingYear);
        var disbursedBefore = before.Raod.Totals.Disbursement;

        var dv = await dvs.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: postingYear, Amount: 50_000m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010",
            BudgetCertified: true, InternalAuditConfirmed: true, EndUserConfirmed: true, AccountantSigned: true));
        await dvs.FireAsync(dv.Name, DvAction.Submit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));
        await dvs.FireAsync(dv.Name, DvAction.Approve, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await dvs.FireAsync(dv.Name, DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));
        await dvs.FireAsync(dv.Name, DvAction.Post, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await dvs.FireAsync(dv.Name, DvAction.Release, new TransitionContext("cash@cvsu", DvRoles.Treasury));

        var after = await Reports(db).BudgetRegistryAsync(postingYear);
        Assert.Equal(disbursedBefore + 50_000m, after.Raod.Totals.Disbursement);
    }

    [Fact]
    public async Task Trial_balance_is_balanced_after_a_released_dv()
    {
        await using var db = fixture.CreateContext();
        var dvs = Dv(db);

        // GL postings take their fiscal year from the posting date (the current year),
        // not the DV's declared budget year, so this report reads the posting year. The
        // shared container may hold other DVs in the same year, so we assert the report
        // BALANCES and CONTAINS this DV's lines rather than equalling a synthetic total.
        var postingYear = DateTime.UtcNow.Year;
        var amount = 73_521m; // an oddball amount so we can find this DV's contribution

        var dv = await dvs.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: postingYear, Amount: amount, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010",
            BudgetCertified: true, InternalAuditConfirmed: true, EndUserConfirmed: true, AccountantSigned: true));
        await dvs.FireAsync(dv.Name, DvAction.Submit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));
        await dvs.FireAsync(dv.Name, DvAction.Approve, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await dvs.FireAsync(dv.Name, DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));
        await dvs.FireAsync(dv.Name, DvAction.Post, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await dvs.FireAsync(dv.Name, DvAction.Release, new TransitionContext("cash@cvsu", DvRoles.Treasury));

        var report = await Reports(db).TrialBalanceAsync(postingYear);

        // The whole book balances to the centavo (R-GL-01 across every account).
        Assert.True(report.IsBalanced, $"debit {report.TotalDebit} != credit {report.TotalCredit}");

        // The expense object account (DR) and Cash-MDS (CR) each carry this DV's amount;
        // the payable nets out across the accrual + cash legs.
        var expense = report.Lines.Single(l => l.Account == "50203010");
        Assert.True(expense.Debit >= amount, $"expense debit {expense.Debit} should include {amount}");
        var cash = report.Lines.Single(l => l.Account == "1010404000");
        Assert.True(cash.Credit >= amount, $"cash credit {cash.Credit} should include {amount}");
    }
}
