namespace CvSU.Ais.Domain.Ledgers;

/// <summary>Which side of the books an amount lands on.</summary>
public enum LedgerSide
{
    Debit,
    Credit,
}

/// <summary>
/// The kinds of memorandum entry the budget registry records. The budget
/// registry is cash-basis and distinct from the accrual GL — obligations live
/// here and never touch <c>GeneralLedgerEntry</c>.
/// </summary>
public enum BudgetEntryType
{
    Appropriation,
    AppropriationReversal,
    Allotment,
    AllotmentReversal,
    Obligation,
    ObligationReversal,
    NotYetDueAndDemandable,
    DueAndDemandable,
    Disbursement,
    DisbursementReversal,
}

/// <summary>
/// The canonical debit/credit side for each budget entry type, lifted verbatim
/// from CLAUDE.md §4A.7. Centralising it here means a posting can never write an
/// entry to the wrong side: the side is computed from the type, not supplied by
/// the caller. (Reversals post to the opposite side of the entry they undo.)
/// </summary>
public static class EntryTypeSide
{
    private static readonly IReadOnlyDictionary<BudgetEntryType, LedgerSide> Map =
        new Dictionary<BudgetEntryType, LedgerSide>
        {
            [BudgetEntryType.Appropriation] = LedgerSide.Credit,
            [BudgetEntryType.AppropriationReversal] = LedgerSide.Debit,
            [BudgetEntryType.Allotment] = LedgerSide.Debit,
            [BudgetEntryType.AllotmentReversal] = LedgerSide.Credit,
            [BudgetEntryType.Obligation] = LedgerSide.Credit,
            [BudgetEntryType.ObligationReversal] = LedgerSide.Debit,
            [BudgetEntryType.NotYetDueAndDemandable] = LedgerSide.Debit,
            [BudgetEntryType.DueAndDemandable] = LedgerSide.Debit,
            [BudgetEntryType.Disbursement] = LedgerSide.Debit,
            [BudgetEntryType.DisbursementReversal] = LedgerSide.Credit,
        };

    public static LedgerSide For(BudgetEntryType entryType) => Map[entryType];
}
