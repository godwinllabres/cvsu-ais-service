using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

/// <summary>
/// Computes the COA registries directly off the ledgers (no balance tables — the
/// ledger is the truth). The year's rows are pulled to the application with a
/// single indexed, year-scoped read (the (fiscal_year, …) composite indexes
/// serve these shapes), then folded into the registry lines in memory. The
/// result set per year is bounded, so the in-memory rollup is cheap and sidesteps
/// the EF translation limits around grouped record/conditional-sum projections.
/// </summary>
public sealed class ReportingQueries(AisDbContext db) : IReportingQueries
{
    /// <summary>Flat budget-ledger row joined to its cluster code, for one year.</summary>
    private sealed record BudgetFact(
        string ClusterCode, ExpenseClass ExpenseClass, BudgetEntryType EntryType, decimal Debit, decimal Credit);

    private async Task<List<BudgetFact>> BudgetFactsAsync(int fiscalYear, CancellationToken cancellationToken) =>
        await db.BudgetLedger
            .Where(e => e.FiscalYear == fiscalYear)
            .Join(db.FundingSources,
                e => e.FundingSourceCode, fs => fs.Code,
                (e, fs) => new BudgetFact(fs.ClusterCode, e.ExpenseClass, e.EntryType, e.Debit, e.Credit))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BudgetRegistryRow>> BudgetRegistryAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var facts = await BudgetFactsAsync(fiscalYear, cancellationToken);

        return facts
            .GroupBy(f => new { f.ClusterCode, f.ExpenseClass })
            .Select(g =>
            {
                var cluster = FundCluster.FromCode(g.Key.ClusterCode);
                return new BudgetRegistryRow(
                    cluster.Code, cluster.Name, cluster.RegistryType, g.Key.ExpenseClass,
                    Allotment: Net(g, BudgetEntryType.Allotment, BudgetEntryType.AllotmentReversal),
                    Obligation: Net(g, BudgetEntryType.Obligation, BudgetEntryType.ObligationReversal),
                    Disbursement: Net(g, BudgetEntryType.Disbursement, BudgetEntryType.DisbursementReversal));
            })
            .OrderBy(r => r.FundClusterCode).ThenBy(r => r.ExpenseClass)
            .ToList();
    }

    public async Task<IReadOnlyList<AppropriationAllotmentRow>> AppropriationAllotmentAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var facts = await BudgetFactsAsync(fiscalYear, cancellationToken);

        return facts
            .GroupBy(f => new { f.ClusterCode, f.ExpenseClass })
            .Select(g =>
            {
                var cluster = FundCluster.FromCode(g.Key.ClusterCode);
                return new AppropriationAllotmentRow(
                    cluster.Code, cluster.Name, g.Key.ExpenseClass,
                    Appropriation: Net(g, BudgetEntryType.Appropriation, BudgetEntryType.AppropriationReversal),
                    Allotment: Net(g, BudgetEntryType.Allotment, BudgetEntryType.AllotmentReversal));
            })
            .OrderBy(r => r.FundClusterCode).ThenBy(r => r.ExpenseClass)
            .ToList();
    }

    public async Task<IReadOnlyList<TrialBalanceRow>> TrialBalanceAsync(
        int fiscalYear, CancellationToken cancellationToken = default)
    {
        var rows = await db.GeneralLedger
            .Where(e => e.FiscalYear == fiscalYear)
            .Select(e => new { e.Account, e.Debit, e.Credit })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(e => e.Account)
            .Select(g => new TrialBalanceRow(g.Key, g.Sum(x => x.Debit), g.Sum(x => x.Credit)))
            .OrderBy(r => r.Account)
            .ToList();
    }

    /// <summary>Net amount for a normal/reversal entry-type pair: the normal type
    /// posts on its declared side, the reversal on the opposite, so the net is
    /// (normal side total) − (reversal side total).</summary>
    private static decimal Net(
        IEnumerable<BudgetFact> facts, BudgetEntryType normal, BudgetEntryType reversal)
    {
        var side = EntryTypeSide.For(normal);
        decimal Amount(BudgetFact f) => side == LedgerSide.Debit ? f.Debit : f.Credit;
        decimal ReversalAmount(BudgetFact f) => side == LedgerSide.Debit ? f.Credit : f.Debit;

        return facts.Where(f => f.EntryType == normal).Sum(Amount)
             - facts.Where(f => f.EntryType == reversal).Sum(ReversalAmount);
    }
}
