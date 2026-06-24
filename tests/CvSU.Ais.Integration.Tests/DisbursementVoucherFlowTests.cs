using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>End-to-end through the application service against real Postgres:
/// gapless numbering, transactional create, persisted transitions and SoD —
/// everything but the HTTP layer.</summary>
[Collection("postgres")]
public class DisbursementVoucherFlowTests(PostgresFixture fixture)
{
    private static DisbursementVoucherService Service(AisDbContext db)
    {
        var catalog = new FundingSourceCatalog(db);
        return new DisbursementVoucherService(
            new DisbursementVoucherRepository(db, catalog),
            catalog,
            new GaplessVoucherNumberService(db),
            new UnitOfWork(db));
    }

    [Fact]
    public async Task Create_submit_approve_persists_across_contexts()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 5000m, FundingSourceCode: "01101101",
            BudgetCertified: true, InternalAuditConfirmed: true, EndUserConfirmed: true, AccountantSigned: true));

        Assert.StartsWith("DV-2026-", created.Name);
        Assert.Equal("Draft", created.Status);

        await service.FireAsync(created.Name, DvAction.Submit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));
        var approved = await service.FireAsync(
            created.Name, DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));

        Assert.Equal("Approved", approved.Status);
        Assert.Equal("accountant@cvsu", approved.ApprovedBy);

        // Reload from a fresh context to prove the state actually persisted.
        await using var freshDb = fixture.CreateContext();
        var reloaded = await Service(freshDb).GetAsync(created.Name);
        Assert.Equal("Approved", reloaded.Status);
    }

    [Fact]
    public async Task Encoder_cannot_approve_their_own_dv_through_the_service()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            "self@cvsu", 2026, 1000m, "01101101", true, true, true, true));
        await service.FireAsync(created.Name, DvAction.Submit, new TransitionContext("self@cvsu", DvRoles.Encoder));

        await Assert.ThrowsAsync<SegregationOfDutiesException>(() =>
            service.FireAsync(created.Name, DvAction.Approve, new TransitionContext("self@cvsu", DvRoles.Accountant)));
    }
}
