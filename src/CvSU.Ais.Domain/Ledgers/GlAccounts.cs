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

    /// <summary>Cash – Collecting Officers (RCA): where collections are debited on receipt.
    /// The default paid-to control account when a receipt doesn't name a specific cash account.</summary>
    public const string CashCollectingOfficers = "1010101000";

    /// <summary>Income / collections clearing (credited on receipt). A real chart maps the credit
    /// to the specific income account per fee type; the POC posts to one clearing control account,
    /// keeping the journal balanced and the mapping a later refinement.</summary>
    public const string CollectionsClearing = "4060100000";
}
