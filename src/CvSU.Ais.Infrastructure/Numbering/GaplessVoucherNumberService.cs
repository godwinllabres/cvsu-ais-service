using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Numbering;

/// <summary>
/// Gapless voucher numbering via a counter row serialized by a Postgres
/// transaction-scoped advisory lock (<c>pg_advisory_xact_lock</c>). The lock
/// auto-releases when the ambient transaction ends, and because the counter
/// increment lives inside that transaction, a business rollback rolls the
/// increment back too — so no number is burned. This is the counter-table
/// approach the risk register calls for, not a bare sequence.
/// </summary>
public sealed class GaplessVoucherNumberService(AisDbContext db) : IVoucherNumberGenerator
{
    public async Task<string> NextAsync(string series, CancellationToken cancellationToken = default)
    {
        // Serialize concurrent issuers of this series. Folds the series name into a
        // stable bigint key; the lock is held until the surrounding transaction ends.
        var lockKey = StableHash(series);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

        var counter = await db.Set<VoucherCounter>()
            .FirstOrDefaultAsync(c => c.Series == series, cancellationToken);

        if (counter is null)
        {
            counter = new VoucherCounter { Series = series, Current = 0 };
            db.Add(counter);
        }

        counter.Current += 1;
        await db.SaveChangesAsync(cancellationToken);

        return $"{series}-{counter.Current:D5}";
    }

    private static long StableHash(string value)
    {
        // FNV-1a 64-bit — deterministic across processes (unlike string.GetHashCode).
        unchecked
        {
            ulong hash = 14695981039346656037UL;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 1099511628211UL;
            }
            return (long)hash;
        }
    }
}
