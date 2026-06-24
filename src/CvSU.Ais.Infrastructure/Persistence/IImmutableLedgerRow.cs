namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>
/// Marks a persistence row that, once inserted, may never be updated or deleted.
/// The <see cref="Interceptors.LedgerImmutabilityInterceptor"/> enforces this in
/// the change tracker; the database additionally revokes UPDATE/DELETE grants.
/// </summary>
public interface IImmutableLedgerRow
{
}
