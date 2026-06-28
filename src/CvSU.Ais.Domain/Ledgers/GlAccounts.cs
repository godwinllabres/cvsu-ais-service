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

    /// <summary>Cash on Hand – Collecting Officers (collection receipts).</summary>
    public const string CashOnHand = "1010101000";

    /// <summary>Cash in Bank – Local Currency Current Account (deposit to bank).</summary>
    public const string CashInBankLcca = "1010201000";

    /// <summary>Advances to Special Disbursing Officers (cash advance granted).</summary>
    public const string AdvancesToSdo = "1990101000";

    /// <summary>Due to BIR – Expanded Withholding Tax payable.</summary>
    public const string DueToBir = "2020101000";

    /// <summary>Due to GSIS – government share + employee share payable.</summary>
    public const string DueToGsis = "2020201000";

    /// <summary>Due to PhilHealth – premium contribution payable.</summary>
    public const string DueToPhilhealth = "2020301000";

    /// <summary>Due to Pag-IBIG – fund contribution payable.</summary>
    public const string DueToPagibig = "2020401000";

    /// <summary>Other Payables – catch-all for miscellaneous payroll deductions.</summary>
    public const string OtherPayables = "2990101000";

    /// <summary>Service Income / Tuition and Other School Fees (CVSU collections).</summary>
    public const string ServiceIncome = "4010301000";

    /// <summary>Salaries and Wages – Regular (Personnel Services).</summary>
    public const string SalariesAndWagesRegular = "5010101001";

    /// <summary>Wages – Casual / Job Order / Contract of Service.</summary>
    public const string WagesJoCos = "5010201001";

    /// <summary>Miscellaneous Expense – catch-all when no specific account is supplied.</summary>
    public const string MiscExpense = "5029999099";
}
