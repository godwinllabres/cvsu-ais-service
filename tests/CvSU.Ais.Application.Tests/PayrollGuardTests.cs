using CvSU.Ais.Application.Payroll;

namespace CvSU.Ais.Application.Tests;

/// <summary>
/// F4 — regular payroll cannot be posted with no imported figures (gross ≤ 0), and
/// a balanced payroll posts exactly one balanced GL batch. Create-time guards reject
/// negative totals before the row is written.
/// </summary>
public class PayrollGuardTests
{
    private static PayrollEntryDetailView Detail(
        string status,
        decimal gross,
        decimal net,
        decimal tax = 0m,
        decimal gsis = 0m,
        decimal pagibig = 0m,
        decimal philhealth = 0m,
        decimal otherDeductions = 0m) =>
        new(
            Name: "PAY-2026-0001",
            Title: "June 2026 Regular Payroll",
            PayrollType: "Regular",
            PayrollPeriod: "2026-06",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            ImportStatus: "Imported",
            TotalRecords: 10,
            TotalGrossPay: gross,
            TotalNetPay: net,
            TotalTaxWithheld: tax,
            TotalGsis: gsis,
            TotalPagibig: pagibig,
            TotalPhilhealth: philhealth,
            TotalOtherDeductions: otherDeductions,
            Status: status,
            GlPostingReference: null,
            ValidationErrors: null,
            Remarks: null,
            LoanDeductions: []);

    private static PayrollEntryService Service(
        FakePayrollEntryRepository repo, FakeGeneralLedger gl) =>
        new(repo, gl, new FakeUnitOfWork());

    [Fact]
    public async Task PostAsync_throws_when_TotalGrossPay_is_zero_no_imported_figures()
    {
        var repo = new FakePayrollEntryRepository
        {
            Detail = Detail("Validated", gross: 0m, net: 0m),
        };
        var gl = new FakeGeneralLedger();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo, gl).PostAsync("PAY-2026-0001"));

        Assert.Contains("no imported figures", ex.Message);
        Assert.Empty(gl.Batches);
    }

    [Fact]
    public async Task PostAsync_happy_path_posts_one_balanced_batch()
    {
        // Gross 100,000 = deductions (5,000 tax + 6,000 GSIS + 100 Pag-IBIG + 400 PhilHealth)
        // + net pay 88,500. Balanced by construction.
        var repo = new FakePayrollEntryRepository
        {
            Detail = Detail(
                "Validated",
                gross: 100_000m,
                net: 88_500m,
                tax: 5_000m,
                gsis: 6_000m,
                pagibig: 100m,
                philhealth: 400m),
        };
        var gl = new FakeGeneralLedger();

        await Service(repo, gl).PostAsync("PAY-2026-0001");

        var batch = Assert.Single(gl.Batches);
        Assert.True(batch.IsBalanced);
        Assert.Equal(100_000m, batch.TotalDebit.Amount);
        Assert.Equal(100_000m, batch.TotalCredit.Amount);
        Assert.Contains("Posted", repo.StatusUpdates);
    }

    [Fact]
    public async Task PostAsync_throws_when_net_pay_is_negative()
    {
        var repo = new FakePayrollEntryRepository
        {
            Detail = Detail("Validated", gross: 1_000m, net: -50m),
        };
        var gl = new FakeGeneralLedger();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repo, gl).PostAsync("PAY-2026-0001"));

        Assert.Empty(gl.Batches);
    }

    [Fact]
    public async Task CreateAsync_throws_when_a_deduction_total_is_negative()
    {
        // Service-level create guard (net-pay computation itself is a repository concern,
        // so we assert the guard the service actually owns).
        var repo = new FakePayrollEntryRepository();
        var command = new CreatePayrollEntryCommand(
            PayrollType: "Regular",
            PayrollPeriod: "2026-06",
            PostingDate: new DateOnly(2026, 6, 24),
            FundCluster: "01",
            TotalGrossPay: 100_000m,
            TotalTaxWithheld: -1m,
            TotalGsis: 0m,
            TotalPagibig: 0m,
            TotalPhilhealth: 0m,
            TotalRecords: 10,
            LoanDeductions: [],
            Remarks: null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => Service(repo, new FakeGeneralLedger()).CreateAsync(command));
    }
}
