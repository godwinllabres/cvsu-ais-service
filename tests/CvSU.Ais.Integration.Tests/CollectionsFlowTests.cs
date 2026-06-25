using CvSU.Ais.Application.Collections;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Collections;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>Collections (Official Receipts) against real Postgres: the happy-path receipt + its
/// balanced GL posting, gapless OR numbering, and the offline-replay idempotency that lets a
/// queued receipt sync more than once without double-recording. This is the audit-critical core
/// the offline outbox (desktop, slice 2) depends on.</summary>
[Collection("postgres")]
public class CollectionsFlowTests(PostgresFixture fixture)
{
    private static CollectionsService Service(AisDbContext db) =>
        new(new ReceiptStore(db),
            new GeneralLedgerRepository(db),
            new GaplessVoucherNumberService(db),
            new UnitOfWork(db));

    private static RecordReceiptRequest Receipt(
        string key, decimal amount, string feeType = "Tuition", string fundCluster = "01",
        DateTimeOffset? at = null) => new()
    {
        IdempotencyKey = key,
        Payor = "Juan dela Cruz",
        AmountPaid = amount,
        Mode = "Cash",
        FeeType = feeType,
        FundCluster = fundCluster,
        ReceivedAtUtc = at ?? new DateTimeOffset(2026, 6, 25, 9, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public async Task Recording_a_receipt_issues_a_number_and_posts_a_balanced_collection_journal()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var view = await service.RecordReceiptAsync(Receipt("key-1", 1_500m));

        Assert.StartsWith("AOR-2026-", view.OrNumber);
        Assert.Equal("Issued", view.Status);
        Assert.Equal(1_500m, view.AmountPaid);

        // A tuition collection posts DR cash / CR Tuition Fees income for the amount received.
        var lines = await db.GeneralLedger
            .Where(g => g.VoucherDoctype == OfficialReceipt.DocType && g.VoucherNo == view.OrNumber)
            .ToListAsync();
        Assert.Equal(2, lines.Count);
        Assert.Equal(1_500m, lines.Where(l => l.Debit > 0).Sum(l => l.Debit));
        Assert.Equal(1_500m, lines.Where(l => l.Credit > 0).Sum(l => l.Credit));
        Assert.Contains(lines, l => l.Account == GlAccounts.CashCollectingOfficers && l.Debit == 1_500m);
        Assert.Contains(lines, l => l.Account == GlAccounts.TuitionFeesIncome && l.Credit == 1_500m);
    }

    [Fact]
    public async Task Tuition_collection_credits_income_not_a_clearing_account()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var view = await service.RecordReceiptAsync(Receipt("tuition-1", 2_000m, feeType: "Tuition", fundCluster: "01"));

        Assert.Equal(GlAccounts.TuitionFeesIncome, view.CreditAccount);
        var credit = await db.GeneralLedger.SingleAsync(g => g.VoucherNo == view.OrNumber && g.Credit > 0);
        Assert.Equal(GlAccounts.TuitionFeesIncome, credit.Account);
    }

    [Fact]
    public async Task Fund_07_trust_collection_credits_a_trust_liability_never_income()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        // Fund 07 (Trust Receipts): money held for others — must credit a LIABILITY, not income.
        var view = await service.RecordReceiptAsync(Receipt("trust-1", 5_000m, feeType: "Fiduciary", fundCluster: "07"));

        Assert.Equal(GlAccounts.TrustLiabilities, view.CreditAccount);
        var credit = await db.GeneralLedger.SingleAsync(g => g.VoucherNo == view.OrNumber && g.Credit > 0);
        Assert.Equal(GlAccounts.TrustLiabilities, credit.Account);
        Assert.NotEqual(GlAccounts.TuitionFeesIncome, credit.Account);
    }

    [Fact]
    public void Fund_07_cluster_overrides_fee_type_to_a_trust_liability()
    {
        // Even a "Tuition" fee_type, if collected into the trust fund, is a trust liability.
        Assert.Equal(GlAccounts.TrustLiabilities,
            CollectionsService.ResolveCreditAccount(FeeType.Tuition, "07"));
        // And an own-source tuition collection stays income.
        Assert.Equal(GlAccounts.TuitionFeesIncome,
            CollectionsService.ResolveCreditAccount(FeeType.Tuition, "01"));
    }

    [Fact]
    public void A_receipt_without_a_credit_account_cannot_be_constructed()
    {
        Assert.Throws<ArgumentException>(() => new OfficialReceipt(
            "Payor", new Money(100m), PaymentMode.Cash, FeeType.Tuition, "01",
            creditAccount: "  ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Or_numbers_are_gapless_and_sequential()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        var first = await service.RecordReceiptAsync(Receipt("seq-1", 100m));
        var second = await service.RecordReceiptAsync(Receipt("seq-2", 200m));
        var third = await service.RecordReceiptAsync(Receipt("seq-3", 300m));

        // Consecutive issue → consecutive numbers, no gaps.
        var n1 = int.Parse(first.OrNumber.Split('-')[^1]);
        var n2 = int.Parse(second.OrNumber.Split('-')[^1]);
        var n3 = int.Parse(third.OrNumber.Split('-')[^1]);
        Assert.Equal(n1 + 1, n2);
        Assert.Equal(n2 + 1, n3);
    }

    [Fact]
    public async Task Replaying_the_same_idempotency_key_records_exactly_once()
    {
        await using var db = fixture.CreateContext();
        var service = Service(db);

        // Simulate an offline receipt that replays twice on reconnect with the SAME key.
        var first = await service.RecordReceiptAsync(Receipt("offline-abc", 999m));
        var replay = await service.RecordReceiptAsync(Receipt("offline-abc", 999m));

        // Same OR number returned; no second receipt, no second GL posting.
        Assert.Equal(first.OrNumber, replay.OrNumber);

        var receiptCount = await db.Set<Infrastructure.Persistence.OfficialReceiptRow>()
            .CountAsync(r => r.IdempotencyKey == "offline-abc");
        Assert.Equal(1, receiptCount);

        var glLineCount = await db.GeneralLedger
            .CountAsync(g => g.VoucherDoctype == OfficialReceipt.DocType && g.VoucherNo == first.OrNumber);
        Assert.Equal(2, glLineCount); // exactly one balanced journal, not two
    }
}
