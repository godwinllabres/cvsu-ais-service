using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Abstractions;

/// <summary>
/// The budget registry, read and appended through the budget ledger itself
/// (the ledger is the source of truth — there is no separate balance table to
/// drift). The <c>Lock*</c> reads take a <c>SELECT … FOR UPDATE</c> on the
/// relevant ledger rows so concurrent allotment/obligation never overruns a
/// ceiling (R-BUD-01/02).
/// </summary>
public interface IBudgetLedger
{
    Task<AppropriationSnapshot?> LockAppropriationAsync(string appropriationId, CancellationToken cancellationToken = default);

    Task<AllotmentSnapshot?> LockAllotmentAsync(string allotmentId, CancellationToken cancellationToken = default);

    Task AppendAsync(BudgetLedgerEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>Locked view of an appropriation's running balance, rebuilt from the ledger.</summary>
public sealed record AppropriationSnapshot(
    string Id,
    int FiscalYear,
    UacsCode Uacs,
    Money FinalAppropriation,
    Money Allotted);

/// <summary>Locked view of an allotment's running balance, rebuilt from the ledger.</summary>
public sealed record AllotmentSnapshot(
    string Id,
    AppropriationSnapshot Appropriation,
    Money Amount,
    DateOnly ReleaseDate,
    Money Obligated);
