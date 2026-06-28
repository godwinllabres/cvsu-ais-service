using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.Abstractions;

/// <summary>
/// Read-only reporting projections, computed straight from the two ledgers (the
/// ledgers are the source of truth — there are no balance tables to drift). All
/// reads are scoped to a fiscal year; the budget reports group by the UACS
/// dimensions the official COA registries are organised around.
/// </summary>
public interface IReportingQueries
{
    /// <summary>Budget-ledger rollup per fund cluster + expense class for one fiscal year
    /// (the RAOD/RBUD shape: allotments, obligations, disbursements, and balances).</summary>
    Task<IReadOnlyList<BudgetRegistryRow>> BudgetRegistryAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>Appropriation-vs-allotment rollup per fund cluster + expense class (the RAPAL shape).</summary>
    Task<IReadOnlyList<AppropriationAllotmentRow>> AppropriationAllotmentAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>GL debit/credit totals per account for one fiscal year (the trial-balance shape).</summary>
    Task<IReadOnlyList<TrialBalanceRow>> TrialBalanceAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>GL account balances classified by RCA major account group, for the financial
    /// statements (Statement of Financial Position and Financial Performance).</summary>
    Task<IReadOnlyList<AccountBalanceRow>> AccountBalancesAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>Revenue recognised per RCA revenue account, split by quarter of the posting date —
    /// the shape of the RROR and the Quarterly Report of Revenue and Other Receipts (FAR No. 5).</summary>
    Task<IReadOnlyList<RevenueByQuarterRow>> RevenueByQuarterAsync(int fiscalYear, CancellationToken cancellationToken = default);

    /// <summary>Disbursements totalled per allotment class (PS/MOOE/FinEx/CO) from the budget
    /// ledger's disbursement entries — the column structure of the Monthly Report of
    /// Disbursements (FAR No. 4).</summary>
    Task<IReadOnlyList<DisbursementByClassRow>> DisbursementsByClassAsync(int fiscalYear, CancellationToken cancellationToken = default);
}

/// <summary>One RROR/QRROR line: a revenue account and the amount recognised per quarter.</summary>
public sealed record RevenueByQuarterRow(
    string Account, decimal Q1, decimal Q2, decimal Q3, decimal Q4)
{
    public decimal Total => Q1 + Q2 + Q3 + Q4;
}

/// <summary>Disbursements totalled per allotment class — the FAR No. 4 column structure.</summary>
public sealed record DisbursementByClassRow(ExpenseClass ExpenseClass, decimal Amount);

/// <summary>The RCA major account groups, keyed off the first digit of the account code
/// (Revised Chart of Accounts): 1 Assets · 2 Liabilities · 3 Equity · 4 Revenue · 5 Expenses.</summary>
public enum RcaGroup
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense,
    Other,
}

/// <summary>One classified GL account with its net balance. Assets/Expenses carry debit
/// balances; Liabilities/Equity/Revenue carry credit balances — the financial statements
/// present each on its natural side.</summary>
public sealed record AccountBalanceRow(string Account, RcaGroup Group, decimal Debit, decimal Credit)
{
    public decimal Net => Debit - Credit;
}

/// <summary>One RAOD/RBUD line: the obligation/disbursement position of a fund-cluster + expense-class slice.</summary>
public sealed record BudgetRegistryRow(
    string FundClusterCode,
    string FundClusterName,
    RegistryType Registry,
    ExpenseClass ExpenseClass,
    decimal Allotment,
    decimal Obligation,
    decimal Disbursement)
{
    /// <summary>Allotment not yet obligated.</summary>
    public decimal UnobligatedBalance => Allotment - Obligation;

    /// <summary>Obligated but not yet disbursed (unpaid obligations).</summary>
    public decimal UnpaidObligation => Obligation - Disbursement;
}

/// <summary>One RAPAL line: appropriation vs allotment for a fund-cluster + expense-class slice.</summary>
public sealed record AppropriationAllotmentRow(
    string FundClusterCode,
    string FundClusterName,
    ExpenseClass ExpenseClass,
    decimal Appropriation,
    decimal Allotment)
{
    public decimal UnallottedBalance => Appropriation - Allotment;
}

/// <summary>One trial-balance line: total debits and credits posted to a GL account.</summary>
public sealed record TrialBalanceRow(string Account, decimal Debit, decimal Credit);
