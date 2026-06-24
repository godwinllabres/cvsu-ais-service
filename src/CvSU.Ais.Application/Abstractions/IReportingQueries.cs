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
