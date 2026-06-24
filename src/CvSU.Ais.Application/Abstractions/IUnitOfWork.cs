namespace CvSU.Ais.Application.Abstractions;

/// <summary>Runs a unit of work inside a single database transaction, so a
/// gapless voucher number and the rows that consume it commit or roll back
/// together.</summary>
public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
