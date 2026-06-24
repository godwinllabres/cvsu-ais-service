using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Domain.Ledgers;

/// <summary>
/// One immutable, append-only budget-registry line. The side (debit/credit) is
/// derived from the <see cref="BudgetEntryType"/> via <see cref="EntryTypeSide"/>,
/// so it is impossible to post an entry to the wrong side — a strictly stronger
/// guarantee than the legacy controller's runtime validator. Reversals are new
/// rows of the opposite type, never edits.
///
/// This is the budget book, kept distinct from <see cref="GeneralLedgerEntry"/>.
/// Obligations are memorandum entries here and must never reach the accrual GL.
/// </summary>
public sealed record BudgetLedgerEntry
{
    public DateOnly PostingDate { get; }
    public int FiscalYear { get; }
    public UacsCode Uacs { get; }
    public BudgetEntryType EntryType { get; }
    public Money Debit { get; }
    public Money Credit { get; }
    public string VoucherDoctype { get; }
    public string VoucherNo { get; }
    public string? AppropriationId { get; }
    public string? AllotmentId { get; }

    public BudgetLedgerEntry(
        DateOnly postingDate,
        int fiscalYear,
        UacsCode uacs,
        BudgetEntryType entryType,
        Money amount,
        string voucherDoctype,
        string voucherNo,
        string? appropriationId = null,
        string? allotmentId = null)
    {
        if (!amount.IsPositive)
            throw new SingleSidedViolationException(
                $"Budget ledger amount must be positive (got {amount}).");

        var side = EntryTypeSide.For(entryType);
        PostingDate = postingDate;
        FiscalYear = fiscalYear;
        Uacs = uacs ?? throw new ArgumentNullException(nameof(uacs));
        EntryType = entryType;
        Debit = side == LedgerSide.Debit ? amount : Money.Zero;
        Credit = side == LedgerSide.Credit ? amount : Money.Zero;
        VoucherDoctype = voucherDoctype;
        VoucherNo = voucherNo;
        AppropriationId = appropriationId;
        AllotmentId = allotmentId;
    }

    public LedgerSide Side => EntryTypeSide.For(EntryType);

    /// <summary>The non-zero amount on whichever side this entry posts to.</summary>
    public Money Amount => Debit.IsPositive ? Debit : Credit;

    public FundCluster Cluster => Uacs.Cluster;
}
