using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class FundingSourceCatalog(AisDbContext db) : IFundingSourceCatalog
{
    public async Task<FundingSource?> FindAsync(string code, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<FundingSourceRow>()
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

        return row is null
            ? null
            : new FundingSource(row.Code, row.Name, FundCluster.FromCode(row.ClusterCode));
    }
}
