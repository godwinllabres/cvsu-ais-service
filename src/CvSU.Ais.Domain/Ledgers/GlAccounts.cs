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

    // ── Collection credit accounts (the leg credited on receipt) ────────────────────────────
    // A collection is DR cash / CR <one of these>, resolved from (fee_type, fund cluster). These
    // are real RCA codes from the UACS chart (data/uacs_chart.csv), NOT a placeholder clearing
    // account. The Fund-07 / fiduciary case credits a LIABILITY, never income — trust receipts are
    // money held for others, not the SUC's earned revenue (CLAUDE.md §4A.2: Fund 07 = Trust, N/A).

    /// <summary>Tuition Fees income (RCA 40202010). Own-source revenue — Fund 01 tuition collection.</summary>
    public const string TuitionFeesIncome = "40202010";

    /// <summary>Other Service Income (RCA 40201990). Default own-source revenue for "Other" fees.</summary>
    public const string OtherServiceIncome = "40201990";

    /// <summary>Guaranty/Security Deposits Payable (RCA 20401040) — the trust LIABILITY credited for
    /// Fund 07 / fiduciary / student-org collections. The agency holds these for others; they are
    /// reversed on refund/disbursement and are never recognised as income.</summary>
    public const string TrustLiabilities = "20401040";
}
