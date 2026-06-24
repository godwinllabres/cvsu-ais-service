using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.Abstractions;

/// <summary>Resolves stored funding-source reference data into the rich domain
/// <see cref="FundingSource"/> (with its cluster behaviour rebuilt in code).</summary>
public interface IFundingSourceCatalog
{
    Task<FundingSource?> FindAsync(string code, CancellationToken cancellationToken = default);
}
