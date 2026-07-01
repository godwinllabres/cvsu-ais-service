using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Payroll;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public sealed record PayrollLoanDeductionDto(
    string LoanType,
    string? LoanReference,
    string EmployeeId,
    string? EmployeeName,
    decimal Amount);

public sealed record PayrollEntryView(
    string Name,
    string PayrollType,
    string PayrollPeriod,
    DateOnly PostingDate,
    decimal TotalNetPay,
    string Status);

public sealed record PayrollEntryDetailView(
    string Name,
    string? Title,
    string PayrollType,
    string PayrollPeriod,
    DateOnly PostingDate,
    string? FundCluster,
    string ImportStatus,
    int TotalRecords,
    decimal TotalGrossPay,
    decimal TotalNetPay,
    decimal TotalTaxWithheld,
    decimal TotalGsis,
    decimal TotalPagibig,
    decimal TotalPhilhealth,
    decimal TotalOtherDeductions,
    string Status,
    string? GlPostingReference,
    string? ValidationErrors,
    string? Remarks,
    IReadOnlyList<PayrollLoanDeductionDto> LoanDeductions);

public sealed record CreatePayrollEntryCommand(
    string PayrollType,
    string PayrollPeriod,
    DateOnly PostingDate,
    string? FundCluster,
    decimal TotalGrossPay,
    decimal TotalTaxWithheld,
    decimal TotalGsis,
    decimal TotalPagibig,
    decimal TotalPhilhealth,
    int TotalRecords,
    IReadOnlyList<PayrollLoanDeductionDto> LoanDeductions,
    string? Remarks);

// ─── JO/COS DTOs ─────────────────────────────────────────────────────────────

public sealed record JoCosPayrollLineDto(
    string EmployeeId,
    string EmployeeName,
    string EmploymentType,
    decimal AuthorizedHours,
    decimal ActualHours,
    decimal TardinessHours,
    decimal DailyRate,
    decimal GrossPay,
    decimal NetPay,
    string? Remarks);

public sealed record JoCosPayrollView(
    string Name,
    string EmployeeType,
    string PayrollPeriod,
    decimal TotalGross,
    decimal TotalNet,
    string Status);

public sealed record JoCosPayrollDetailView(
    string Name,
    string EmployeeType,
    string PayrollPeriod,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    DateOnly PostingDate,
    string? FundCluster,
    string? HrTransmittalReference,
    DateOnly? HrTransmittalReceivedDate,
    decimal TotalHours,
    decimal TotalDays,
    decimal TotalGross,
    decimal TotalNet,
    bool HoursValidated,
    bool DtrValidated,
    bool AccomplishmentValidated,
    string? ValidationRemarks,
    string Status,
    string? GlPostingReference,
    string? Remarks,
    IReadOnlyList<JoCosPayrollLineDto> Lines);

public sealed record CreateJoCosPayrollCommand(
    string EmployeeType,
    string PayrollPeriod,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo,
    DateOnly PostingDate,
    string? FundCluster,
    IReadOnlyList<JoCosPayrollLineDto> Lines,
    string? Remarks);

// ─── Salary Tranche DTOs ──────────────────────────────────────────────────────

public sealed record SalaryTrancheEntryDto(
    int SalaryGrade,
    int Step,
    decimal MonthlySalary);

public sealed record SalaryTrancheView(
    string Name,
    string SslLaw,
    int TrancheNumber,
    int EffectiveYear,
    bool IsActive,
    string Status);

public sealed record SalaryTrancheDetailView(
    string Name,
    string SslLaw,
    int TrancheNumber,
    int EffectiveYear,
    DateOnly? EffectiveDate,
    string? DbmCircularReference,
    bool IsActive,
    int TotalEntries,
    decimal MinSalary,
    decimal MaxSalary,
    string Status,
    string ImportStatus,
    string? ValidationErrors,
    string? Remarks,
    IReadOnlyList<SalaryTrancheEntryDto> Entries);

public sealed record CreateSalaryTrancheCommand(
    string SslLaw,
    int TrancheNumber,
    int EffectiveYear,
    DateOnly? EffectiveDate,
    string? DbmCircularReference,
    IReadOnlyList<SalaryTrancheEntryDto>? Entries,
    string? Remarks);

// ─── Payroll Entry Service ────────────────────────────────────────────────────

/// <summary>
/// Orchestrates regular government payroll lifecycle. <see cref="PostAsync"/> posts
/// the full payroll GL:
/// <list type="bullet">
///   <item>Dr. Salaries and Wages (5010101001) = TotalGrossPay</item>
///   <item>Cr. Due to GSIS / PhilHealth / Pag-IBIG / BIR = respective totals</item>
///   <item>Cr. Other Payables = TotalOtherDeductions</item>
///   <item>Cr. Cash–MDS Regular = TotalNetPay</item>
/// </list>
/// </summary>
public sealed class PayrollEntryService(
    IPayrollEntryRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "Payroll Entry";

    public Task<IReadOnlyList<PayrollEntryView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<PayrollEntryDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Payroll entry '{name}' not found.");

    public Task<PayrollEntryView> CreateAsync(
        CreatePayrollEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TotalGrossPay < 0m)
            throw new ArgumentException("Total gross pay cannot be negative.", nameof(command));
        if (command.TotalTaxWithheld < 0m)
            throw new ArgumentException("Total tax withheld cannot be negative.", nameof(command));
        if (command.TotalGsis < 0m)
            throw new ArgumentException("Total GSIS cannot be negative.", nameof(command));
        if (command.TotalPagibig < 0m)
            throw new ArgumentException("Total Pag-IBIG cannot be negative.", nameof(command));
        if (command.TotalPhilhealth < 0m)
            throw new ArgumentException("Total PhilHealth cannot be negative.", nameof(command));
        if (command.TotalRecords < 0)
            throw new ArgumentException("Total records cannot be negative.", nameof(command));

        return repo.AddAsync(command, cancellationToken);
    }

    public async Task ValidateAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Payroll entry '{name}' not found.");
        if (entry.Status != "Draft")
            throw new InvalidOperationException($"Payroll entry '{name}' must be Draft to validate.");
        await repo.UpdateStatusAsync(name, "Validated", cancellationToken);
    }

    /// <summary>
    /// Posts the validated payroll to the accrual GL in one balanced batch.
    /// Only non-zero deduction totals produce credit lines; zero amounts are skipped
    /// to avoid constructing invalid (both-zero) GL entries.
    /// </summary>
    public Task PostAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var entry = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"Payroll entry '{name}' not found.");

            if (entry.Status != "Validated")
                throw new InvalidOperationException(
                    $"Payroll entry '{name}' must be Validated before posting (current: {entry.Status}).");

            if (entry.TotalGrossPay <= 0m)
                throw new InvalidOperationException("Payroll has no imported figures to post.");

            if (entry.TotalNetPay < 0m)
                throw new InvalidOperationException(
                    $"Payroll entry '{name}' has a negative net pay and cannot be posted.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fiscalYear = today.Year;
            var gross = new Money(entry.TotalGrossPay);

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.SalariesAndWagesRegular, gross, Money.Zero,
                    DocType, name, $"Payroll {entry.PayrollPeriod}"));

            void AddCredit(string account, decimal amount, string remarks)
            {
                if (amount > 0)
                    batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                        account, Money.Zero, new Money(amount), DocType, name, remarks));
            }

            AddCredit(GlAccounts.DueToGsis,       entry.TotalGsis,             "GSIS contribution");
            AddCredit(GlAccounts.DueToPhilhealth,  entry.TotalPhilhealth,       "PhilHealth premium");
            AddCredit(GlAccounts.DueToPagibig,     entry.TotalPagibig,          "Pag-IBIG contribution");
            AddCredit(GlAccounts.DueToBir,         entry.TotalTaxWithheld,      "EWT / BIR withholding tax");
            AddCredit(GlAccounts.OtherPayables,    entry.TotalOtherDeductions,  "Other payroll deductions");
            AddCredit(GlAccounts.CashMdsRegular,   entry.TotalNetPay,           "Net pay disbursement");

            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateStatusAsync(name, "Posted", token);
            return 0;
        }, cancellationToken);

    public async Task CancelAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Payroll entry '{name}' not found.");
        if (entry.Status == "Posted")
            throw new InvalidOperationException($"Payroll entry '{name}' is already posted and cannot be cancelled.");
        await repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
    }
}

// ─── JO/COS Payroll Service ──────────────────────────────────────────────────

/// <summary>
/// Orchestrates JO/COS payroll. <see cref="ComputeAsync"/> recalculates each line's
/// computed hours (actual − tardiness), gross pay (computed × daily rate), and net pay
/// before allowing the batch to be posted. <see cref="PostAsync"/> then writes:
/// Dr. Wages–Casual (5010201001) = TotalGross / Cr. Cash–MDS = TotalNet
/// (plus Cr. Other Payables if there are deductions).
/// </summary>
public sealed class JoCosPayrollService(
    IJoCosPayrollRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "JO COS Payroll";

    public Task<IReadOnlyList<JoCosPayrollView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<JoCosPayrollDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");

    public Task<JoCosPayrollView> CreateAsync(
        CreateJoCosPayrollCommand command,
        CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public async Task ValidateHoursAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");
        if (entry.Status != "Draft" && entry.Status != "HrTransmittalReceived")
            throw new InvalidOperationException(
                $"JO/COS payroll entry '{name}' cannot be hour-validated in status '{entry.Status}'.");
        await repo.UpdateStatusAsync(name, "HoursValidated", cancellationToken);
    }

    /// <summary>
    /// Recomputes each line: computed_hours = max(0, actual − tardiness),
    /// gross_pay = computed_hours × daily_rate, net_pay = gross_pay.
    /// Updates all line records and the batch totals, then transitions to Computed.
    /// </summary>
    public Task ComputeAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var entry = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");

            if (entry.Status != "HoursValidated")
                throw new InvalidOperationException(
                    $"JO/COS payroll entry '{name}' must be HoursValidated before computing (current: {entry.Status}).");

            decimal totalHours = 0, totalGross = 0, totalNet = 0;

            foreach (var line in entry.Lines)
            {
                var computed = Math.Max(0m, line.ActualHours - line.TardinessHours);
                var gross = Math.Round(computed * line.DailyRate, 2, MidpointRounding.AwayFromZero);
                var net = gross;

                await repo.UpdateLineComputedAsync(name, line.EmployeeId, computed, gross, net, token);

                totalHours += computed;
                totalGross += gross;
                totalNet += net;
            }

            var totalDays = entry.Lines.Any() ? totalHours / 8m : 0m;

            await repo.UpdateComputedTotalsAsync(name, totalHours, totalDays, totalGross, totalNet, token);
            await repo.UpdateStatusAsync(name, "Computed", token);
            return 0;
        }, cancellationToken);

    public async Task ApproveAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");
        if (entry.Status != "Computed")
            throw new InvalidOperationException(
                $"JO/COS payroll entry '{name}' must be Computed before approving (current: {entry.Status}).");
        await repo.UpdateStatusAsync(name, "Approved", cancellationToken);
    }

    /// <summary>
    /// Posts the approved JO/COS payroll to the GL:
    /// Dr. Wages–Casual / Cr. Cash–MDS (net) + Cr. Other Payables (deductions if any).
    /// </summary>
    public Task PostAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var entry = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");

            if (entry.Status != "Approved")
                throw new InvalidOperationException(
                    $"JO/COS payroll entry '{name}' must be Approved before posting (current: {entry.Status}).");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fiscalYear = today.Year;
            var gross = new Money(entry.TotalGross);
            var net = new Money(entry.TotalNet);
            var deductions = new Money(entry.TotalGross - entry.TotalNet);

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.WagesJoCos, gross, Money.Zero,
                    DocType, name, $"JO/COS payroll {entry.PayrollPeriod}"));

            if (deductions.IsPositive)
                batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.OtherPayables, Money.Zero, deductions,
                    DocType, name, "JO/COS payroll deductions"));

            batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                GlAccounts.CashMdsRegular, Money.Zero, net,
                DocType, name, "JO/COS net pay disbursement"));

            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateStatusAsync(name, "Posted", token);
            return 0;
        }, cancellationToken);

    public async Task CancelAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"JO/COS payroll entry '{name}' not found.");
        if (entry.Status == "Posted")
            throw new InvalidOperationException(
                $"JO/COS payroll entry '{name}' is already posted and cannot be cancelled.");
        await repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
    }
}

// ─── Salary Tranche Service ───────────────────────────────────────────────────

public sealed class SalaryTrancheService(ISalaryTrancheRepository repo)
{
    public Task<IReadOnlyList<SalaryTrancheView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<SalaryTrancheDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Salary tranche '{name}' not found.");

    public Task<SalaryTrancheView> CreateAsync(
        CreateSalaryTrancheCommand command,
        CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public async Task ActivateAsync(string name, CancellationToken cancellationToken = default)
    {
        var tranche = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Salary tranche '{name}' not found.");
        if (tranche.Status != "Draft")
            throw new InvalidOperationException($"Salary tranche '{name}' must be Draft to activate.");
        await repo.UpdateStatusAsync(name, "Active", isActive: true, cancellationToken);
    }

    public async Task SupersedeAsync(string name, CancellationToken cancellationToken = default)
    {
        var tranche = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Salary tranche '{name}' not found.");
        if (tranche.Status != "Active")
            throw new InvalidOperationException($"Salary tranche '{name}' must be Active to supersede.");
        await repo.UpdateStatusAsync(name, "Superseded", isActive: false, cancellationToken);
    }
}
