namespace CvSU.Ais.Domain.Funds;

/// <summary>
/// One of the seven Philippine government fund clusters.
///
/// This is the keystone of the migration's defect-#2 fix. In the legacy
/// Frappe app the DV stored both <c>funding_source</c> and a free-standing
/// <c>fund_cluster</c> field, derived from <c>operational_fund</c> and never
/// cross-validated — so the two could silently diverge. Here the cluster is a
/// closed set of value objects, and the only way to reach one is through a
/// <see cref="FundingSource"/>. There is no independently-settable cluster
/// field anywhere in the model, so divergence is structurally impossible.
/// </summary>
public sealed record FundCluster
{
    /// <summary>Two-digit cluster code, "01".."07".</summary>
    public string Code { get; }

    public string Name { get; }

    public RegistryType RegistryType { get; }

    /// <summary>STF / Internally Generated Funds (cluster 05) cannot pay Personnel
    /// Services; every other cluster can (R-BUD-05).</summary>
    public bool CanFundPersonnelServices { get; }

    private FundCluster(string code, string name, RegistryType registryType, bool canFundPersonnelServices)
    {
        Code = code;
        Name = name;
        RegistryType = registryType;
        CanFundPersonnelServices = canFundPersonnelServices;
    }

    public static readonly FundCluster RegularAgency =
        new("01", "Regular Agency Fund", RegistryType.Raod, canFundPersonnelServices: true);

    public static readonly FundCluster ForeignAssistedProjects =
        new("02", "Foreign Assisted Projects Fund", RegistryType.Raod, canFundPersonnelServices: true);

    public static readonly FundCluster SpecialAccountsLocallyFunded =
        new("03", "Special Accounts – Locally Funded", RegistryType.Raod, canFundPersonnelServices: true);

    public static readonly FundCluster SpecialAccountsForeignAssisted =
        new("04", "Special Accounts – Foreign Assisted", RegistryType.Raod, canFundPersonnelServices: true);

    public static readonly FundCluster InternallyGenerated =
        new("05", "Internally Generated Funds (incl. STF)", RegistryType.Rbud, canFundPersonnelServices: false);

    public static readonly FundCluster BusinessRelated =
        new("06", "Business Related Funds (Revolving)", RegistryType.Rbud, canFundPersonnelServices: true);

    public static readonly FundCluster TrustReceipts =
        new("07", "Trust Receipts", RegistryType.None, canFundPersonnelServices: true);

    private static readonly IReadOnlyDictionary<string, FundCluster> ByCode =
        new[]
        {
            RegularAgency, ForeignAssistedProjects, SpecialAccountsLocallyFunded,
            SpecialAccountsForeignAssisted, InternallyGenerated, BusinessRelated, TrustReceipts,
        }.ToDictionary(c => c.Code);

    public static IReadOnlyCollection<FundCluster> All => (IReadOnlyCollection<FundCluster>)ByCode.Values;

    public static FundCluster FromCode(string code) =>
        ByCode.TryGetValue(code, out var cluster)
            ? cluster
            : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown fund cluster code.");

    public override string ToString() => $"{Code} - {Name}";
}
