using CvSU.Ais.Domain.Collections;

namespace CvSU.Ais.Application.Abstractions;

/// <summary>Persists issued Official Receipts and backs offline-replay idempotency. The store is
/// the single place a receipt row is written, and the place a replayed request is recognised.</summary>
public interface IReceiptStore
{
    /// <summary>Return the OR number a prior request with this idempotency key produced, or null
    /// if the key has not been seen. Lets a replayed offline receipt resolve to its original OR
    /// number instead of creating a second receipt + second GL posting.</summary>
    Task<string?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Append an issued receipt and record its idempotency key in the same transaction.</summary>
    Task AppendAsync(OfficialReceipt receipt, string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>List issued receipts (newest first) for the register / inbox.</summary>
    Task<IReadOnlyList<OfficialReceipt>> ListAsync(CancellationToken cancellationToken = default);
}
