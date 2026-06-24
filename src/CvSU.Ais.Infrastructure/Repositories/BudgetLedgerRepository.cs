using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;
using CvSU.Ais.Infrastructure.Mapping;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

/// <summary>
/// Reads budget balances straight from the budget ledger and appends new entries.
/// The <c>Lock*</c> methods first take a <c>SELECT … FOR UPDATE</c> on every row
/// of the appropriation/allotment, so a concurrent caller blocks until this
/// transaction commits — closing the over-obligation race. Must be called inside
/// a transaction (the unit of work supplies it).
/// </summary>
public sealed class BudgetLedgerRepository(AisDbContext db, IFundingSourceCatalog fundingSources) : IBudgetLedger
{
    public async Task<AppropriationSnapshot?> LockAppropriationAsync(
        string appropriationId, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM budget_ledger_entry WHERE appropriation_id = {0} FOR UPDATE",
            [appropriationId], cancellationToken);

        var appropriationRows = await db.BudgetLedger
            .Where(e => e.AppropriationId == appropriationId && e.EntryType == BudgetEntryType.Appropriation)
            .ToListAsync(cancellationToken);
        if (appropriationRows.Count == 0)
            return null;

        var header = appropriationRows[0];
        var finalAppropriation = appropriationRows.Sum(r => r.Credit);

        var allotted = await db.BudgetLedger
            .Where(e => e.AppropriationId == appropriationId && e.EntryType == BudgetEntryType.Allotment)
            .SumAsync(e => (decimal?)e.Debit, cancellationToken) ?? 0m;

        var uacs = await BuildUacsAsync(header, cancellationToken);
        return new AppropriationSnapshot(
            appropriationId, header.FiscalYear, uacs, new Money(finalAppropriation), new Money(allotted));
    }

    public async Task<AllotmentSnapshot?> LockAllotmentAsync(
        string allotmentId, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM budget_ledger_entry WHERE allotment_id = {0} FOR UPDATE",
            [allotmentId], cancellationToken);

        var allotmentRows = await db.BudgetLedger
            .Where(e => e.AllotmentId == allotmentId && e.EntryType == BudgetEntryType.Allotment)
            .ToListAsync(cancellationToken);
        if (allotmentRows.Count == 0)
            return null;

        var header = allotmentRows[0];
        var amount = allotmentRows.Sum(r => r.Debit);

        var obligated = await db.BudgetLedger
            .Where(e => e.AllotmentId == allotmentId && e.EntryType == BudgetEntryType.Obligation)
            .SumAsync(e => (decimal?)e.Credit, cancellationToken) ?? 0m;

        if (header.AppropriationId is null)
            throw new InvalidOperationException($"Allotment {allotmentId} has no appropriation link.");

        var appropriation = await LockAppropriationAsync(header.AppropriationId, cancellationToken)
            ?? throw new InvalidOperationException($"Appropriation {header.AppropriationId} not found.");

        return new AllotmentSnapshot(
            allotmentId, appropriation, new Money(amount), header.PostingDate, new Money(obligated));
    }

    public async Task AppendAsync(BudgetLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        db.Add(entry.ToRow());
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UacsCode> BuildUacsAsync(BudgetLedgerRow row, CancellationToken cancellationToken)
    {
        var fundingSource = await fundingSources.FindAsync(row.FundingSourceCode, cancellationToken)
            ?? throw new InvalidOperationException($"Unknown funding source {row.FundingSourceCode}.");
        return new UacsCode(fundingSource, row.PapCode, row.LocationCode, row.ExpenseClass, row.ObjectAccountCode);
    }
}
