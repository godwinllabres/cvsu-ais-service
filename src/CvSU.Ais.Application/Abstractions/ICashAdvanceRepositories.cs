using CvSU.Ais.Application.CashAdvances;

namespace CvSU.Ais.Application.Abstractions;

public interface ICashAdvanceRepository
{
    Task<IReadOnlyList<CashAdvanceView>> ListAsync(CancellationToken cancellationToken = default);
    Task<CashAdvanceDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<string> AddAsync(CreateCashAdvanceCommand command, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);

    /// <summary>Stamps the GL posting reference when the advance is disbursed.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default);
}

public interface ILiquidationReportRepository
{
    Task<IReadOnlyList<LiquidationReportView>> ListAsync(CancellationToken cancellationToken = default);
    Task<LiquidationReportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task<string> AddAsync(CreateLiquidationReportCommand command, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);

    /// <summary>Stamps the GL posting reference after liquidation is posted.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the parent cash advance's liquidated/unliquidated balances once a
    /// liquidation report is posted. The repository owns this update to keep the
    /// balance consistent with the posted GL entries.
    /// </summary>
    Task UpdateCashAdvanceLiquidatedAsync(string cashAdvanceName, decimal totalLiquidated, CancellationToken cancellationToken = default);
}
