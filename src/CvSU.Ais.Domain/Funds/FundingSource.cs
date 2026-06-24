namespace CvSU.Ais.Domain.Funds;

/// <summary>
/// A UACS funding source — the single stored fund key for every financial
/// transaction. The fund <see cref="Cluster"/> is intrinsic to the funding
/// source, so <see cref="RegistryType"/> and the STF-PS rule are <em>derived</em>
/// projections rather than separately-stored fields. Callers that previously
/// reached for a <c>fund_cluster</c> column now read <c>FundingSource.Cluster</c>.
/// </summary>
public sealed class FundingSource
{
    /// <summary>UACS funding-source code (e.g. "01101101").</summary>
    public string Code { get; }

    public string Name { get; }

    public FundCluster Cluster { get; }

    public FundingSource(string code, string name, FundCluster cluster)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Funding source code is required.", nameof(code));

        Code = code;
        Name = name ?? string.Empty;
        Cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
    }

    /// <summary>Derived from the cluster — never stored independently.</summary>
    public RegistryType RegistryType => Cluster.RegistryType;

    /// <summary>Derived from the cluster — never stored independently (R-BUD-05).</summary>
    public bool CanFundPersonnelServices => Cluster.CanFundPersonnelServices;

    public override string ToString() => $"{Code} ({Cluster.Code})";
}
