using CvSU.Ais.Application.JournalEntries;

namespace CvSU.Ais.Application.Tests;

/// <summary>
/// F2 — a Journal Entry must not be posted twice, and must not be approved once it
/// has already reached a terminal/posted status. The service reads the persisted
/// detail and rejects re-posting/re-approval regardless of what the UI allows.
/// </summary>
public class JournalEntryGuardTests
{
    private static JournalEntryDetailView Detail(
        string approvalStatus, string? glPostingReference = null) =>
        new(
            Name: "JE-2026-0001",
            Title: "Test JE",
            PostingDate: new DateOnly(2026, 6, 24),
            FiscalYear: 2026,
            FundCluster: "01",
            JeType: "Journal Entry Voucher",
            ApprovalStatus: approvalStatus,
            TotalDebit: 100m,
            TotalCredit: 100m,
            ApprovedBy: null,
            GlPostingReference: glPostingReference,
            UserRemark: null,
            Lines: new List<JeLineDto>
            {
                new("1010101000", "Cash", 100m, 0m, "dr"),
                new("4010301000", "Income", 0m, 100m, "cr"),
            });

    private static JournalEntryService Service(
        FakeJournalEntryRepository repo, FakeGeneralLedger gl) =>
        new(repo, gl, new FakeUnitOfWork());

    [Fact]
    public async Task PostAsync_throws_when_entry_already_Posted()
    {
        var repo = new FakeJournalEntryRepository { Detail = Detail("Posted") };
        var gl = new FakeGeneralLedger();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo, gl).PostAsync("JE-2026-0001"));

        Assert.Empty(gl.Batches);
    }

    [Fact]
    public async Task PostAsync_throws_when_GlPostingReference_already_set()
    {
        // Status is still "Approved" but a GL reference exists → already posted.
        var repo = new FakeJournalEntryRepository
        {
            Detail = Detail("Approved", glPostingReference: "JE-2026-0001"),
        };
        var gl = new FakeGeneralLedger();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo, gl).PostAsync("JE-2026-0001"));

        Assert.Empty(gl.Batches);
    }

    [Fact]
    public async Task ApproveAsync_throws_when_entry_already_Posted()
    {
        var repo = new FakeJournalEntryRepository { Detail = Detail("Posted") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo, new FakeGeneralLedger()).ApproveAsync("JE-2026-0001", "accountant@cvsu"));

        Assert.Null(repo.LastStatus);
    }

    [Fact]
    public async Task PostAsync_happy_path_posts_one_balanced_batch_from_Approved()
    {
        var repo = new FakeJournalEntryRepository { Detail = Detail("Approved") };
        var gl = new FakeGeneralLedger();

        await Service(repo, gl).PostAsync("JE-2026-0001");

        var batch = Assert.Single(gl.Batches);
        Assert.True(batch.IsBalanced);
        Assert.Equal("Posted", repo.LastStatus);
    }
}
