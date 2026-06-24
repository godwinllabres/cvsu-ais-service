using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
            new GeneralLedgerRepository(db),
            new BudgetLedgerRepository(db, catalog),
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
            "self@cvsu", 2026, 1000m, "01101101",
            BudgetCertified: true, InternalAuditConfirmed: true, EndUserConfirmed: true, AccountantSigned: true));
        await service.FireAsync(created.Name, DvAction.Submit, new TransitionContext("self@cvsu", DvRoles.Encoder));

        await Assert.ThrowsAsync<SegregationOfDutiesException>(() =>
            service.FireAsync(created.Name, DvAction.Approve, new TransitionContext("self@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public async Task Post_then_release_emit_the_two_stage_ledger_postings()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var created = await service.CreateAsync(new CreateDvCommand(
            Encoder: "clerk@cvsu", FiscalYear: 2026, Amount: 1000m, FundingSourceCode: "01101101",
            PapCode: "PAP-A", LocationCode: "LOC-A", ExpenseClass: ExpenseClass.Mooe, ObjectAccountCode: "50203010",
            BudgetCertified: true, InternalAuditConfirmed: true, EndUserConfirmed: true, AccountantSigned: true));

        await service.FireAsync(created.Name, DvAction.Submit, new TransitionContext("clerk@cvsu", DvRoles.Encoder));
        await service.FireAsync(created.Name, DvAction.Approve, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        await service.FireAsync(created.Name, DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));

        var posted = await service.FireAsync(created.Name, DvAction.Post, new TransitionContext("acct@cvsu", DvRoles.Accountant));
        Assert.Equal("Posted", posted.Status);

        // Post emits the accrual journal (DR expense / CR payable), which balances.
        await using (var read = fixture.CreateContext())
        {
            var accrual = await read.GeneralLedger.Where(e => e.VoucherNo == created.Name).ToListAsync();
            Assert.Equal(2, accrual.Count);
            Assert.Equal(accrual.Sum(e => e.Debit), accrual.Sum(e => e.Credit));
        }

        var released = await service.FireAsync(created.Name, DvAction.Release, new TransitionContext("cash@cvsu", DvRoles.Treasury));
        Assert.Equal("Released", released.Status);

        // Release adds the cash journal (DR payable / CR cash) plus a budget Disbursement entry.
        await using (var read = fixture.CreateContext())
        {
            var gl = await read.GeneralLedger.Where(e => e.VoucherNo == created.Name).ToListAsync();
            Assert.Equal(4, gl.Count);
            Assert.Equal(gl.Sum(e => e.Debit), gl.Sum(e => e.Credit));

            var disbursement = await read.BudgetLedger
                .Where(e => e.VoucherNo == created.Name && e.EntryType == BudgetEntryType.Disbursement)
                .ToListAsync();
            var entry = Assert.Single(disbursement);
            Assert.Equal(1000m, entry.Debit);
        }
    }
}
