using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.Reports;

/// <summary>
/// Serves the official COA registries straight from the ledgers. The heavy
/// grouping happens in the database (<see cref="IReportingQueries"/>); this
/// service only shapes the result into report views and splits the budget
/// registry into its RAOD (clusters 01–04) and RBUD (05–06) halves the way the
/// COA forms are filed.
/// </summary>
public sealed class ReportingService(IReportingQueries queries)
{
    public async Task<BudgetRegistryReport> BudgetRegistryAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await queries.BudgetRegistryAsync(fiscalYear, cancellationToken);

        var raod = rows.Where(r => r.Registry == RegistryType.Raod).ToList();
        var rbud = rows.Where(r => r.Registry == RegistryType.Rbud).ToList();

        return new BudgetRegistryReport(
            fiscalYear,
            new BudgetRegistrySection("RAOD", raod, Totalize(raod)),
            new BudgetRegistrySection("RBUD", rbud, Totalize(rbud)));
    }

    public async Task<AppropriationAllotmentReport> AppropriationAllotmentAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await queries.AppropriationAllotmentAsync(fiscalYear, cancellationToken);
        return new AppropriationAllotmentReport(
            fiscalYear, rows,
            rows.Sum(r => r.Appropriation), rows.Sum(r => r.Allotment));
    }

    public async Task<TrialBalanceReport> TrialBalanceAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await queries.TrialBalanceAsync(fiscalYear, cancellationToken);
        var debit = rows.Sum(r => r.Debit);
        var credit = rows.Sum(r => r.Credit);
        return new TrialBalanceReport(fiscalYear, rows, debit, credit);
    }

    /// <summary>The four General-Purpose Financial Statements (GAM/PPSAS), derived from the
    /// classified GL account balances: Statement of Financial Position, Statement of Financial
    /// Performance, Statement of Cash Flows, and Statement of Changes in Net Assets/Equity.</summary>
    public async Task<FinancialStatementsReport> FinancialStatementsAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var balances = await queries.AccountBalancesAsync(fiscalYear, cancellationToken);

        // Each account is shown on its natural side: assets/expenses by debit-positive net,
        // liabilities/equity/revenue by credit-positive net (so the figures read as positive).
        static FsLine Line(AccountBalanceRow r, bool creditNatural) =>
            new(r.Account, creditNatural ? -r.Net : r.Net);

        var assets = balances.Where(b => b.Group == RcaGroup.Asset)
            .Select(b => Line(b, creditNatural: false)).ToList();
        var liabilities = balances.Where(b => b.Group == RcaGroup.Liability)
            .Select(b => Line(b, creditNatural: true)).ToList();
        var equity = balances.Where(b => b.Group == RcaGroup.Equity)
            .Select(b => Line(b, creditNatural: true)).ToList();
        var revenue = balances.Where(b => b.Group == RcaGroup.Revenue)
            .Select(b => Line(b, creditNatural: true)).ToList();
        var expenses = balances.Where(b => b.Group == RcaGroup.Expense)
            .Select(b => Line(b, creditNatural: false)).ToList();

        var totalRevenue = revenue.Sum(l => l.Amount);
        var totalExpenses = expenses.Sum(l => l.Amount);
        var surplus = totalRevenue - totalExpenses;

        var totalAssets = assets.Sum(l => l.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var beginningEquity = equity.Sum(l => l.Amount);
        // The period surplus rolls into accumulated net assets/equity (closing equity).
        var endingEquity = beginningEquity + surplus;

        var position = new StatementOfFinancialPosition(
            assets, liabilities, equity, totalAssets, totalLiabilities, endingEquity);

        var performance = new StatementOfFinancialPerformance(
            revenue, expenses, totalRevenue, totalExpenses, surplus);

        // Statement of Changes in Net Assets/Equity: beginning balance + surplus = ending.
        var changes = new StatementOfChangesInEquity(beginningEquity, surplus, endingEquity);

        // Statement of Cash Flows (indirect, simplified): the net change in cash accounts.
        // Cash accounts are RCA assets beginning with "101" (Cash and Cash Equivalents).
        var cashLines = assets.Where(l => l.Account.StartsWith("101")).ToList();
        var netCash = cashLines.Sum(l => l.Amount);
        var cashFlows = new StatementOfCashFlows(cashLines, netCash);

        return new FinancialStatementsReport(fiscalYear, position, performance, changes, cashFlows);
    }

    /// <summary>The Quarterly Report of Revenue and Other Receipts (FAR No. 5) / RROR — revenue
    /// recognised per account, split by quarter, with the annual total.</summary>
    public async Task<RevenueReport> RevenueReportAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await queries.RevenueByQuarterAsync(fiscalYear, cancellationToken);
        return new RevenueReport(fiscalYear, rows,
            rows.Sum(r => r.Q1), rows.Sum(r => r.Q2), rows.Sum(r => r.Q3), rows.Sum(r => r.Q4));
    }

    /// <summary>The Monthly Report of Disbursements (FAR No. 4) — disbursements by allotment class
    /// (PS/MOOE/FinEx/CO). Our DV disbursements settle via MDS Checks under the Notice of Cash
    /// Allocation, so the class totals populate that row; instrument types the service does not
    /// track (NTA, Working Fund, TRA, CDC, NCAA) are reported as zero.</summary>
    public async Task<MonthlyDisbursementReport> MonthlyDisbursementsAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await queries.DisbursementsByClassAsync(fiscalYear, cancellationToken);
        decimal ByClass(ExpenseClass c) => rows.Where(r => r.ExpenseClass == c).Sum(r => r.Amount);

        var ps = ByClass(ExpenseClass.Ps);
        var mooe = ByClass(ExpenseClass.Mooe);
        var fe = ByClass(ExpenseClass.Fe);
        var co = ByClass(ExpenseClass.Co);

        return new MonthlyDisbursementReport(fiscalYear, ps, mooe, fe, co);
    }

    private static BudgetRegistryTotals Totalize(IReadOnlyList<BudgetRegistryRow> rows) =>
        new(rows.Sum(r => r.Allotment), rows.Sum(r => r.Obligation), rows.Sum(r => r.Disbursement));
}

/// <summary>RROR / Quarterly Report of Revenue and Other Receipts (FAR No. 5).</summary>
public sealed record RevenueReport(
    int FiscalYear, IReadOnlyList<RevenueByQuarterRow> Lines,
    decimal TotalQ1, decimal TotalQ2, decimal TotalQ3, decimal TotalQ4)
{
    public decimal GrandTotal => TotalQ1 + TotalQ2 + TotalQ3 + TotalQ4;
}

/// <summary>Monthly Report of Disbursements (FAR No. 4): disbursements by allotment class.
/// <see cref="MdsChecks"/> carries the per-class totals (our disbursements settle via MDS);
/// the per-class TOTAL is the row sum.</summary>
public sealed record MonthlyDisbursementReport(
    int FiscalYear, decimal Ps, decimal Mooe, decimal FinEx, decimal Co)
{
    public decimal Total => Ps + Mooe + FinEx + Co;
}

/// <summary>One financial-statement line: an RCA account and its amount on its natural side.</summary>
public sealed record FsLine(string Account, decimal Amount);

/// <summary>Statement of Financial Position (balance sheet): Assets = Liabilities + Net Assets/Equity.</summary>
public sealed record StatementOfFinancialPosition(
    IReadOnlyList<FsLine> Assets, IReadOnlyList<FsLine> Liabilities, IReadOnlyList<FsLine> Equity,
    decimal TotalAssets, decimal TotalLiabilities, decimal TotalEquity)
{
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;
}

/// <summary>Statement of Financial Performance (income statement): Revenue − Expenses = Surplus/(Deficit).</summary>
public sealed record StatementOfFinancialPerformance(
    IReadOnlyList<FsLine> Revenue, IReadOnlyList<FsLine> Expenses,
    decimal TotalRevenue, decimal TotalExpenses, decimal SurplusDeficit);

/// <summary>Statement of Changes in Net Assets/Equity: beginning + surplus = ending.</summary>
public sealed record StatementOfChangesInEquity(
    decimal BeginningEquity, decimal SurplusDeficit, decimal EndingEquity);

/// <summary>Statement of Cash Flows (simplified): the net movement in cash &amp; cash equivalents.</summary>
public sealed record StatementOfCashFlows(IReadOnlyList<FsLine> CashLines, decimal NetCashFlow);

/// <summary>The four General-Purpose Financial Statements for a fiscal year.</summary>
public sealed record FinancialStatementsReport(
    int FiscalYear,
    StatementOfFinancialPosition Position,
    StatementOfFinancialPerformance Performance,
    StatementOfChangesInEquity Changes,
    StatementOfCashFlows CashFlows);

public sealed record BudgetRegistryReport(
    int FiscalYear, BudgetRegistrySection Raod, BudgetRegistrySection Rbud);

public sealed record BudgetRegistrySection(
    string Registry, IReadOnlyList<BudgetRegistryRow> Lines, BudgetRegistryTotals Totals);

public sealed record BudgetRegistryTotals(decimal Allotment, decimal Obligation, decimal Disbursement)
{
    public decimal UnobligatedBalance => Allotment - Obligation;
    public decimal UnpaidObligation => Obligation - Disbursement;
}

public sealed record AppropriationAllotmentReport(
    int FiscalYear, IReadOnlyList<AppropriationAllotmentRow> Lines,
    decimal TotalAppropriation, decimal TotalAllotment)
{
    public decimal TotalUnallotted => TotalAppropriation - TotalAllotment;
}

public sealed record TrialBalanceReport(
    int FiscalYear, IReadOnlyList<TrialBalanceRow> Lines, decimal TotalDebit, decimal TotalCredit)
{
    /// <summary>A trial balance must balance to the centavo (R-GL-01 across the whole book).</summary>
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}
