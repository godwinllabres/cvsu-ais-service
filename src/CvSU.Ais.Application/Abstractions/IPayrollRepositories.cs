using CvSU.Ais.Application.Payroll;

namespace CvSU.Ais.Application.Abstractions;

public interface IPayrollEntryRepository
{
    Task<IReadOnlyList<PayrollEntryView>> ListAsync(CancellationToken cancellationToken = default);
    Task<PayrollEntryDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<PayrollEntryView> AddAsync(CreatePayrollEntryCommand command, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);

    /// <summary>Stamps the GL posting reference after payroll is posted to the ledger.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default);
}

public interface IJoCosPayrollRepository
{
    Task<IReadOnlyList<JoCosPayrollView>> ListAsync(CancellationToken cancellationToken = default);
    Task<JoCosPayrollDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<JoCosPayrollView> AddAsync(CreateJoCosPayrollCommand command, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a single payroll line's computed values (hours, gross, net) after the
    /// Compute step runs. Identified by (parentName, employeeId) — assumed unique per batch.
    /// </summary>
    Task UpdateLineComputedAsync(
        string parentName, string employeeId,
        decimal computedHours, decimal grossPay, decimal netPay,
        CancellationToken cancellationToken = default);

    /// <summary>Updates the batch-level totals after all lines have been re-computed.</summary>
    Task UpdateComputedTotalsAsync(
        string name,
        decimal totalHours, decimal totalDays, decimal totalGross, decimal totalNet,
        CancellationToken cancellationToken = default);

    /// <summary>Stamps the GL posting reference after payroll is posted.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default);
}

public interface ISalaryTrancheRepository
{
    Task<IReadOnlyList<SalaryTrancheView>> ListAsync(CancellationToken cancellationToken = default);
    Task<SalaryTrancheDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<SalaryTrancheView> AddAsync(CreateSalaryTrancheCommand command, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, bool isActive, CancellationToken cancellationToken = default);
}
