using CvSU.Ais.Domain.Funds;
using Xunit;

namespace CvSU.Ais.Domain.Tests;

/// <summary>
/// Defect #2: the fund dimension is modeled once. There is no independently
/// settable fund-cluster field; everything fund-related is reached through the
/// funding source, so the legacy divergence between <c>funding_source</c> and
/// <c>fund_cluster</c> cannot recur.
/// </summary>
public class FundDimensionTests
{
    [Fact]
    public void RegistryType_is_derived_from_the_cluster_not_stored_separately()
    {
        var regular = TestData.RegularAgencyFund();
        var stf = TestData.StfFund();

        Assert.Equal(RegistryType.Raod, regular.RegistryType);
        Assert.Equal(RegistryType.Rbud, stf.RegistryType);
    }

    [Fact]
    public void Stf_cluster_cannot_fund_personnel_services_every_other_can()
    {
        Assert.False(FundCluster.InternallyGenerated.CanFundPersonnelServices);
        Assert.True(FundCluster.RegularAgency.CanFundPersonnelServices);
    }

    [Theory]
    [InlineData("01", RegistryType.Raod)]
    [InlineData("05", RegistryType.Rbud)]
    [InlineData("07", RegistryType.None)]
    public void FromCode_resolves_the_canonical_singleton(string code, RegistryType expected)
    {
        var cluster = FundCluster.FromCode(code);

        Assert.Equal(expected, cluster.RegistryType);
        Assert.Same(cluster, FundCluster.FromCode(code));
    }

    [Fact]
    public void All_seven_fund_clusters_are_defined()
    {
        Assert.Equal(7, FundCluster.All.Count);
    }

    [Fact]
    public void Unknown_cluster_code_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FundCluster.FromCode("99"));
    }
}
