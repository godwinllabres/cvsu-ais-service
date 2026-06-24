using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Domain.Budget;

/// <summary>
/// An allotment released against an <see cref="Appropriation"/>. Tracks how much
/// of itself has been obligated and is the single place the obligation ceiling
/// (R-BUD-02), the STF Personnel-Services prohibition (R-BUD-05) and fund-cluster
/// integrity are enforced. Construct only via <see cref="Appropriation.Allot"/>.
/// </summary>
public sealed class Allotment
{
    public string Id { get; }
    public Appropriation Appropriation { get; }
    public Money Amount { get; }
    public DateOnly ReleaseDate { get; }

    private Money _obligated = Money.Zero;

    internal Allotment(string id, Appropriation appropriation, Money amount, DateOnly releaseDate)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Allotment id is required.", nameof(id));

        Id = id;
        Appropriation = appropriation;
        Amount = amount;
        ReleaseDate = releaseDate;
    }

    public FundingSource FundingSource => Appropriation.FundingSource;

    public Money ObligatedAmount => _obligated;
    public Money UnobligatedBalance => Amount - _obligated;

    /// <summary>Reconstitute from persisted state, seeding the running obligated
    /// total read back from the budget ledger (the persistence boundary).</summary>
    public static Allotment Rehydrate(
        string id, Appropriation appropriation, Money amount, DateOnly releaseDate, Money obligatedSoFar)
    {
        var allotment = new Allotment(id, appropriation, amount, releaseDate);
        allotment._obligated = obligatedSoFar;
        return allotment;
    }

    /// <summary>The budget-ledger row recorded when this allotment is submitted.</summary>
    public BudgetLedgerEntry CreatePosting(DateOnly postingDate) =>
        new(postingDate, Appropriation.FiscalYear, Appropriation.Uacs, BudgetEntryType.Allotment,
            Amount, nameof(Allotment), Id, appropriationId: Appropriation.Id, allotmentId: Id);

    /// <summary>
    /// Record an obligation (ORS/BURS line) against this allotment. Enforces, in
    /// order: STF cannot fund PS (R-BUD-05), no cross-fund-cluster contamination,
    /// then the obligation ceiling (R-BUD-02). Returns the budget-ledger entry to
    /// be appended; never touches the accrual GL.
    /// </summary>
    public BudgetLedgerEntry Obligate(string obligationVoucherNo, Money amount, UacsCode uacs, DateOnly postingDate)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Obligation must be positive.");

        if (!FundingSource.CanFundPersonnelServices && uacs.ExpenseClass == ExpenseClass.Ps)
            throw new StfCannotFundPersonnelServicesException(
                $"Fund cluster {FundingSource.Cluster.Code} cannot fund Personnel Services (R-BUD-05). " +
                "Charge PS to a Regular Agency Fund allotment instead.");

        if (uacs.Cluster != FundingSource.Cluster)
            throw new FundClusterContaminationException(
                $"Obligation fund cluster {uacs.Cluster.Code} differs from allotment cluster " +
                $"{FundingSource.Cluster.Code}; a single obligation cannot mix fund clusters. " +
                "Raise a separate ORS/BURS per cluster.");

        if (amount > UnobligatedBalance)
            throw new BudgetCeilingExceededException(
                $"Obligation {amount} exceeds unobligated balance {UnobligatedBalance} of allotment {Id} " +
                "(R-BUD-02). Reduce the obligation or secure additional allotment.");

        _obligated += amount;
        return new BudgetLedgerEntry(
            postingDate, Appropriation.FiscalYear, uacs, BudgetEntryType.Obligation,
            amount, "AIS ORS BURS", obligationVoucherNo,
            appropriationId: Appropriation.Id, allotmentId: Id);
    }
}
