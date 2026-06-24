using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure.Mapping;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class GeneralLedgerRepository(AisDbContext db) : IGeneralLedger
{
    public async Task AppendBatchAsync(GlPostingBatch batch, CancellationToken cancellationToken = default)
    {
        batch.EnsureBalanced(); // R-GL-01 — refuse to persist an unbalanced journal.

        foreach (var entry in batch.Entries)
            db.Add(entry.ToRow());

        await db.SaveChangesAsync(cancellationToken);
    }
}
