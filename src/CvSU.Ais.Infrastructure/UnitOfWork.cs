using CvSU.Ais.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure;

public sealed class UnitOfWork(AisDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
