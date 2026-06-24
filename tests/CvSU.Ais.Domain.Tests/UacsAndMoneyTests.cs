using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using Xunit;

namespace CvSU.Ais.Domain.Tests;

public class UacsCompletenessTests
{
    private readonly FundingSource _fs = TestData.RegularAgencyFund();

    [Fact]
    public void Complete_tuple_is_accepted()
    {
        var uacs = new UacsCode(_fs, "PAP", "LOC", ExpenseClass.Mooe, "50203010");
        Assert.Equal(FundCluster.RegularAgency, uacs.Cluster);
    }

    [Theory]
    [InlineData("", "LOC", "50203010")]
    [InlineData("PAP", "", "50203010")]
    [InlineData("PAP", "LOC", "")]
    public void Missing_any_dimension_is_rejected_R_BUD_06(string pap, string loc, string account)
    {
        Assert.Throws<UacsIncompleteException>(
            () => new UacsCode(_fs, pap, loc, ExpenseClass.Mooe, account));
    }

    [Fact]
    public void Missing_funding_source_is_rejected()
    {
        Assert.Throws<UacsIncompleteException>(
            () => new UacsCode(null!, "PAP", "LOC", ExpenseClass.Mooe, "50203010"));
    }
}

public class MoneyTests
{
    [Fact]
    public void Rounds_to_centavos_away_from_zero()
    {
        Assert.Equal(10.13m, new Money(10.125m).Amount);
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(new Money(100m), new Money(100.00m));
    }

    [Fact]
    public void One_centavo_difference_is_within_tolerance()
    {
        Assert.True(new Money(1000m).EqualsWithinTolerance(new Money(1000.01m)));
        Assert.False(new Money(1000m).EqualsWithinTolerance(new Money(1000.02m)));
    }
}
