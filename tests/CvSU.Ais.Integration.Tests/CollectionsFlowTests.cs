using CvSU.Ais.Application.Collections;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Collections;
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

    private static RecordReceiptRequest Receipt(string key, decimal amount, DateTimeOffset? at = null) => new()
    {
        IdempotencyKey = key,
        Payor = "Juan dela Cruz",
        AmountPaid = amount,
        Mode = "Cash",
        FundCluster = "01",
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

        // The collection posted DR cash / CR collections-clearing for the amount received.
        var lines = await db.GeneralLedger
            .Where(g => g.VoucherDoctype == OfficialReceipt.DocType && g.VoucherNo == view.OrNumber)
            .ToListAsync();
        Assert.Equal(2, lines.Count);
        Assert.Equal(1_500m, lines.Where(l => l.Debit > 0).Sum(l => l.Debit));
        Assert.Equal(1_500m, lines.Where(l => l.Credit > 0).Sum(l => l.Credit));
        Assert.Contains(lines, l => l.Account == GlAccounts.CashCollectingOfficers && l.Debit == 1_500m);
        Assert.Contains(lines, l => l.Account == GlAccounts.CollectionsClearing && l.Credit == 1_500m);
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
