using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Abstractions;

/// <summary>Appends balanced journals to the accrual general ledger. Append-only —
/// posted rows are immutable (enforced by the persistence interceptor).</summary>
public interface IGeneralLedger
{
    Task AppendBatchAsync(GlPostingBatch batch, CancellationToken cancellationToken = default);
}
