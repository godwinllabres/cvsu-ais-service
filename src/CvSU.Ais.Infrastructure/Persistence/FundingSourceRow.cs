namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Funding-source reference data. Only the cluster <em>code</em> is
/// stored; the rich <c>FundCluster</c> value object is rebuilt in code via
/// <c>FundCluster.FromCode</c>, so the cluster's behaviour never duplicates into
/// the database.</summary>
public sealed class FundingSourceRow
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ClusterCode { get; set; } = default!;
}
