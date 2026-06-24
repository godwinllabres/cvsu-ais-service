using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Domain.Tests;

/// <summary>Shared fixtures for the invariant suite.</summary>
internal static class TestData
{
    public static FundingSource RegularAgencyFund(string code = "01101101") =>
        new(code, "Regular Agency Fund", FundCluster.RegularAgency);

    public static FundingSource StfFund(string code = "05101101") =>
        new(code, "Internally Generated Funds (STF)", FundCluster.InternallyGenerated);

    public static UacsCode Uacs(FundingSource fundingSource, ExpenseClass expenseClass = ExpenseClass.Mooe) =>
        new(fundingSource, "100000100001000", "0102301000000", expenseClass, "50203010");

    public static readonly DateOnly Today = new(2026, 6, 24);
    public const int FiscalYear = 2026;
}
