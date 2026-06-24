namespace CvSU.Ais.Domain.Ledgers;

/// <summary>
/// The handful of RCA object accounts the DV posting touches. Real charts are
/// reference data, but the payable and Modified Disbursement System cash accounts
/// are fixed control accounts, so they live here as named constants.
/// </summary>
public static class GlAccounts
{
    /// <summary>Accounts Payable (RCA control account).</summary>
    public const string AccountsPayable = "2010101000";

    /// <summary>Cash – Modified Disbursement System (MDS), Regular.</summary>
    public const string CashMdsRegular = "1010404000";
}
