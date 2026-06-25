using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Contracts;
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

    private static BudgetRegistryTotals Totalize(IReadOnlyList<BudgetRegistryRow> rows) =>
        new(rows.Sum(r => r.Allotment), rows.Sum(r => r.Obligation), rows.Sum(r => r.Disbursement));
}

// BudgetRegistryReport/Section/Totals, AppropriationAllotmentReport and TrialBalanceReport
// now come from the shared CvSU.Ais.Contracts project (derived fields are computed
// properties there, so the client renders the same numbers the server computes).
