using CvSU.Ais.Domain.Common;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CvSU.Ais.Infrastructure.Interceptors;

/// <summary>
/// Reinforces ledger immutability (R-GL-02) at the persistence boundary: any
/// attempt to UPDATE or DELETE a posted ledger row is rejected before it reaches
/// the database. Inserts are allowed (the ledgers are append-only). Combined with
/// the database revoking UPDATE/DELETE grants, corrections can only ever be new
/// reversing rows — strictly stronger than the legacy controller's runtime throw.
/// </summary>
public sealed class LedgerImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        GuardAppendOnly(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        GuardAppendOnly(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void GuardAppendOnly(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<IImmutableLedgerRow>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new LedgerImmutabilityException(
                    $"Ledger rows are immutable (R-GL-02); attempted {entry.State} on " +
                    $"{entry.Entity.GetType().Name}. Post a reversing entry instead of editing.");
        }
    }
}
