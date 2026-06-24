using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Domain.Budget;

/// <summary>
/// An appropriation (GAA / SARO / STF approved budget) — the top of the
/// execution cycle. Tracks how much of itself has been released as allotments
/// and refuses to over-allot (R-BUD-01). Allotments can only be created through
/// <see cref="Allot"/>, which is the single place the ceiling is checked.
/// </summary>
public sealed class Appropriation
{
    public string Id { get; }
    public int FiscalYear { get; }
    public UacsCode Uacs { get; }
    public Money FinalAppropriation { get; }

    private Money _allotted = Money.Zero;

    public Appropriation(string id, int fiscalYear, UacsCode uacs, Money finalAppropriation)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Appropriation id is required.", nameof(id));
        if (!finalAppropriation.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(finalAppropriation), "Appropriation must be positive.");

        Id = id;
        FiscalYear = fiscalYear;
        Uacs = uacs ?? throw new ArgumentNullException(nameof(uacs));
        FinalAppropriation = finalAppropriation;
    }

    public FundingSource FundingSource => Uacs.FundingSource;

    public Money AllottedAmount => _allotted;
    public Money UnallottedBalance => FinalAppropriation - _allotted;

    /// <summary>Reconstitute from persisted state — seeds the running allotted
    /// total read back from the budget ledger so ceiling checks remain correct
    /// across requests. The persistence boundary; callers other than the
    /// repository should not need this.</summary>
    public static Appropriation Rehydrate(
        string id, int fiscalYear, UacsCode uacs, Money finalAppropriation, Money allottedSoFar)
    {
        var appropriation = new Appropriation(id, fiscalYear, uacs, finalAppropriation);
        appropriation._allotted = allottedSoFar;
        return appropriation;
    }

    /// <summary>The budget-ledger row recorded when this appropriation is submitted.</summary>
    public BudgetLedgerEntry CreatePosting(DateOnly postingDate) =>
        new(postingDate, FiscalYear, Uacs, BudgetEntryType.Appropriation,
            FinalAppropriation, nameof(Appropriation), Id, appropriationId: Id);

    /// <summary>
    /// Release part of this appropriation as an allotment, enforcing R-BUD-01:
    /// cumulative allotments may not exceed the final appropriation.
    /// </summary>
    public Allotment Allot(string allotmentId, Money amount, DateOnly releaseDate)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Allotment must be positive.");
        if (amount > UnallottedBalance)
            throw new BudgetCeilingExceededException(
                $"Allotment {amount} exceeds unallotted balance {UnallottedBalance} of appropriation {Id} " +
                "(R-BUD-01). Reduce the allotment or secure a supplemental appropriation.");

        _allotted += amount;
        return new Allotment(allotmentId, this, amount, releaseDate);
    }
}
