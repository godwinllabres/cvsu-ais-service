using CvSU.Ais.Application.Budget;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>The Appropriation → Allotment → Obligation cycle against real Postgres,
/// including the cumulative ceilings (R-BUD-01/02) read back from the ledger.</summary>
[Collection("postgres")]
public class BudgetExecutionFlowTests(PostgresFixture fixture)
{
    private static BudgetExecutionService Service(AisDbContext db)
    {
        var catalog = new FundingSourceCatalog(db);
        return new BudgetExecutionService(
            new BudgetLedgerRepository(db, catalog),
            catalog,
            new GaplessVoucherNumberService(db),
            new UnitOfWork(db));
    }

    private static CreateAppropriationCommand Appropriation(decimal amount) => new(
        FiscalYear: 2026, FundingSourceCode: "01101101", PapCode: "PAP-A", LocationCode: "LOC-A",
        ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010", FinalAppropriation: amount);

    [Fact]
    public async Task Appropriate_allot_obligate_tracks_balances_through_the_ledger()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var appropriation = await service.CreateAppropriationAsync(Appropriation(1_000_000m));
        Assert.Equal(1_000_000m, appropriation.Unallotted);

        var allotment = await service.AllotAsync(appropriation.Id, 600_000m);
        Assert.Equal(600_000m, allotment.Amount);
        Assert.Equal(600_000m, allotment.Unobligated);

        var obligation = await service.ObligateAsync(allotment.Id, 200_000m);
        Assert.Equal(400_000m, obligation.AllotmentUnobligatedBalance);
    }

    [Fact]
    public async Task Cumulative_allotment_cannot_exceed_the_appropriation_R_BUD_01()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var appropriation = await service.CreateAppropriationAsync(Appropriation(1_000_000m));
        await service.AllotAsync(appropriation.Id, 800_000m);

        await Assert.ThrowsAsync<BudgetCeilingExceededException>(
            () => service.AllotAsync(appropriation.Id, 300_000m));
    }

    [Fact]
    public async Task Obligation_cannot_exceed_the_allotment_R_BUD_02()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var appropriation = await service.CreateAppropriationAsync(Appropriation(1_000_000m));
        var allotment = await service.AllotAsync(appropriation.Id, 500_000m);

        await Assert.ThrowsAsync<BudgetCeilingExceededException>(
            () => service.ObligateAsync(allotment.Id, 500_001m));
    }
}
