using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;
using Xunit;

namespace CvSU.Ais.Domain.Tests;

/// <summary>
/// The crown-jewel invariants: single-sided lines, balanced journals, and the
/// entry-type → side convention (CLAUDE.md §4A.7) enforced in code.
/// </summary>
public class LedgerInvariantTests
{
    private static GeneralLedgerEntry Gl(decimal debit, decimal credit) =>
        new(TestData.Today, TestData.FiscalYear, "50203010",
            new Money(debit), new Money(credit), "AIS Journal Entry", "JE-001");

    [Fact]
    public void Gl_line_must_be_debit_xor_credit_not_both()
    {
        Assert.Throws<SingleSidedViolationException>(() => Gl(debit: 100m, credit: 100m));
    }

    [Fact]
    public void Gl_line_must_be_debit_xor_credit_not_neither()
    {
        Assert.Throws<SingleSidedViolationException>(() => Gl(debit: 0m, credit: 0m));
    }

    [Fact]
    public void Gl_line_rejects_negative_amounts()
    {
        Assert.Throws<SingleSidedViolationException>(() => Gl(debit: -100m, credit: 0m));
    }

    [Fact]
    public void Balanced_journal_passes()
    {
        var batch = new GlPostingBatch()
            .Add(Gl(debit: 1000m, credit: 0m))
            .Add(Gl(debit: 0m, credit: 1000m));

        Assert.True(batch.IsBalanced);
        batch.EnsureBalanced();
    }

    [Fact]
    public void Unbalanced_journal_is_rejected()
    {
        var batch = new GlPostingBatch()
            .Add(Gl(debit: 1000m, credit: 0m))
            .Add(Gl(debit: 0m, credit: 999m));

        Assert.False(batch.IsBalanced);
        Assert.Throws<UnbalancedBatchException>(batch.EnsureBalanced);
    }

    [Fact]
    public void Balance_check_tolerates_one_centavo()
    {
        var batch = new GlPostingBatch()
            .Add(Gl(debit: 1000.00m, credit: 0m))
            .Add(Gl(debit: 0m, credit: 999.99m));

        Assert.True(batch.IsBalanced);
    }

    [Theory]
    [InlineData(BudgetEntryType.Appropriation, LedgerSide.Credit)]
    [InlineData(BudgetEntryType.Allotment, LedgerSide.Debit)]
    [InlineData(BudgetEntryType.Obligation, LedgerSide.Credit)]
    [InlineData(BudgetEntryType.ObligationReversal, LedgerSide.Debit)]
    [InlineData(BudgetEntryType.NotYetDueAndDemandable, LedgerSide.Debit)]
    [InlineData(BudgetEntryType.DueAndDemandable, LedgerSide.Debit)]
    [InlineData(BudgetEntryType.Disbursement, LedgerSide.Debit)]
    [InlineData(BudgetEntryType.DisbursementReversal, LedgerSide.Credit)]
    public void Budget_entry_type_posts_to_its_canonical_side(BudgetEntryType type, LedgerSide expected)
    {
        Assert.Equal(expected, EntryTypeSide.For(type));
    }

    [Fact]
    public void Budget_entry_side_is_derived_from_type_so_it_cannot_be_miswritten()
    {
        var entry = new BudgetLedgerEntry(
            TestData.Today, TestData.FiscalYear, TestData.Uacs(TestData.RegularAgencyFund()),
            BudgetEntryType.Allotment, new Money(5000m), "AIS Allotment", "ALL-001");

        Assert.Equal(new Money(5000m), entry.Debit);
        Assert.True(entry.Credit.IsZero);
        Assert.Equal(LedgerSide.Debit, entry.Side);
    }
}
