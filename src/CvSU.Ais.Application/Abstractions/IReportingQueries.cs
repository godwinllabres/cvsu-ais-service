using CvSU.Ais.Contracts;

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

// BudgetRegistryRow, AppropriationAllotmentRow and TrialBalanceRow now come from the
// shared CvSU.Ais.Contracts project (referenced by both the API and the Blazor client).
