using CvSU.Ais.Application.CashAdvances;

namespace CvSU.Ais.Application.Tests;

/// <summary>
/// F18 — a liquidation report cannot be posted against a cash advance that is not
/// currently "Advanced" (e.g. already "FullyLiquidated"), preventing a second
/// liquidation from crediting Advances-to-SDO twice.
/// F30 — a cash advance cannot be created with a non-positive amount.
/// </summary>
public class CashAdvanceGuardTests
{
    private static CashAdvanceDetailView Advance(string status, decimal amount = 10_000m) =>
        new(
            Name: "CA-2026-0001",
            Employee: "emp-001",
            EmployeeName: "Juan Dela Cruz",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            Purpose: "Travel",
            AdvanceAmount: amount,
            LiquidatedAmount: 0m,
            UnliquidatedBalance: amount,
            DueDate: new DateOnly(2026, 7, 24),
            Status: status,
            GlPostingReference: null,
            Remarks: null);

    private static LiquidationReportDetailView Liquidation(
        string status = "Submitted",
        decimal totalLiquidated = 8_000m,
        decimal advanceAmount = 10_000m) =>
        new(
            Name: "LR-2026-0001",
            CashAdvanceName: "CA-2026-0001",
            Employee: "emp-001",
            EmployeeName: "Juan Dela Cruz",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            TotalLiquidated: totalLiquidated,
            AdvanceAmount: advanceAmount,
            RefundDue: advanceAmount - totalLiquidated,
            ReimbursementDue: 0m,
            Status: status,
            GlPostingReference: null,
            Remarks: null,
            Lines: new List<LiquidationLineDto>
            {
                new("Travel", "Fare", totalLiquidated, "R-1", new DateOnly(2026, 6, 20), "5029999099"),
            });

    [Fact]
    public async Task LiquidationPost_throws_when_parent_advance_already_FullyLiquidated()
    {
        var liqRepo = new FakeLiquidationReportRepository { Detail = Liquidation() };
        var caRepo = new FakeCashAdvanceRepository { Detail = Advance("FullyLiquidated") };
        var gl = new FakeGeneralLedger();
        var service = new LiquidationReportService(liqRepo, caRepo, gl, new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PostAsync("LR-2026-0001"));

        Assert.Empty(gl.Batches);
    }

    [Fact]
    public async Task LiquidationPost_happy_path_posts_balanced_batch_and_settles_parent()
    {
        var liqRepo = new FakeLiquidationReportRepository { Detail = Liquidation() };
        var caRepo = new FakeCashAdvanceRepository { Detail = Advance("Advanced") };
        var gl = new FakeGeneralLedger();
        var service = new LiquidationReportService(liqRepo, caRepo, gl, new FakeUnitOfWork());

        await service.PostAsync("LR-2026-0001");

        var batch = Assert.Single(gl.Batches);
        Assert.True(batch.IsBalanced);
        Assert.Contains("FullyLiquidated", caRepo.StatusUpdates);
    }

    [Fact]
    public async Task CashAdvanceCreate_throws_when_amount_is_zero()
    {
        var caRepo = new FakeCashAdvanceRepository();
        var service = new CashAdvanceService(caRepo, new FakeGeneralLedger(), new FakeUnitOfWork());
        var command = new CreateCashAdvanceCommand(
            Employee: "emp-001",
            EmployeeName: "Juan Dela Cruz",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            Purpose: "Travel",
            AdvanceAmount: 0m,
            DueDate: new DateOnly(2026, 7, 24),
            Remarks: null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(command));
    }

    [Fact]
    public async Task CashAdvanceCreate_throws_when_amount_is_negative()
    {
        var caRepo = new FakeCashAdvanceRepository();
        var service = new CashAdvanceService(caRepo, new FakeGeneralLedger(), new FakeUnitOfWork());
        var command = new CreateCashAdvanceCommand(
            Employee: "emp-001",
            EmployeeName: "Juan Dela Cruz",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            Purpose: "Travel",
            AdvanceAmount: -1m,
            DueDate: new DateOnly(2026, 7, 24),
            Remarks: null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(command));
    }
}
