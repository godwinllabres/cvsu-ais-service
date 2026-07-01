using CvSU.Ais.Application.Obligations;

namespace CvSU.Ais.Application.Tests;

/// <summary>
/// F1 — ORS/BURS status changes go through one transition table. There is no
/// backward move (FundVerified → Reviewed) and no way to obligate an ORS twice.
/// F22 — a request cannot be created with a non-positive amount.
/// </summary>
public class ObligationRequestGuardTests
{
    private static OrsDetailView Detail(string status, decimal amount = 1000m) =>
        new(
            Name: "ORS-2026-0001",
            PostingDate: new DateOnly(2026, 6, 24),
            FiscalYear: 2026,
            RequestingUnit: "College of Engineering",
            Purpose: "Supplies",
            Amount: amount,
            FundingSourceCode: "01101101",
            PapCode: null,
            LocationCode: null,
            ExpenseClass: null,
            Status: status,
            RequestingOfficeUser: null,
            BudgetOfficerUser: null,
            Remarks: null,
            LineItems: []);

    private static ObligationRequestService Service(FakeObligationRequestRepository repo) =>
        new(repo, new FakeBudgetLedger(), new FakeUnitOfWork());

    private static CreateOrsCommand CreateCommand(decimal amount) =>
        new(
            RequestingUnit: "College of Engineering",
            PostingDate: new DateOnly(2026, 6, 24),
            FiscalYear: 2026,
            Purpose: "Supplies",
            Amount: amount,
            FundingSourceCode: "01101101",
            PapCode: null,
            LocationCode: null,
            ExpenseClass: null,
            RequestingOfficeUser: null,
            BudgetOfficerUser: null,
            LineItems: [],
            Remarks: null);

    [Fact]
    public async Task ReviewAsync_throws_when_already_FundVerified_no_backward_move()
    {
        // Reviewed is not a legal target from FundVerified — this is the backward move.
        var repo = new FakeObligationRequestRepository { Detail = Detail("FundVerified") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo).ReviewAsync("ORS-2026-0001"));

        Assert.Empty(repo.StatusUpdates);
    }

    [Fact]
    public async Task FundVerifyAsync_throws_when_already_FundVerified_no_double_obligation()
    {
        // FundVerified is terminal-forward here (only Signed/Cancelled follow), so a second
        // FundVerify is rejected at the transition guard before the budget ledger is touched.
        var repo = new FakeObligationRequestRepository { Detail = Detail("FundVerified") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo).FundVerifyAsync("ORS-2026-0001", "ALLOT-001"));

        Assert.Empty(repo.StatusUpdates);
    }

    [Fact]
    public async Task CreateAsync_throws_when_amount_is_zero()
    {
        var repo = new FakeObligationRequestRepository();

        await Assert.ThrowsAsync<ArgumentException>(
            () => Service(repo).CreateAsync(CreateCommand(0m)));
    }

    [Fact]
    public async Task CreateAsync_throws_when_amount_is_negative()
    {
        var repo = new FakeObligationRequestRepository();

        await Assert.ThrowsAsync<ArgumentException>(
            () => Service(repo).CreateAsync(CreateCommand(-500m)));
    }
}
