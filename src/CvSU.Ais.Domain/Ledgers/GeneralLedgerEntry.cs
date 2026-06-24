using CvSU.Ais.Domain.Common;

namespace CvSU.Ais.Domain.Ledgers;

/// <summary>
/// One accrual general-ledger line. Immutable by construction (no setters, no
/// mutating methods) — corrections are made by posting a reversing line, never
/// by editing this one. The infrastructure layer reinforces this at the database
/// (revoked UPDATE/DELETE + a SaveChanges interceptor); the domain guarantees a
/// posted line cannot be mutated in memory either.
///
/// Each line is strictly debit-XOR-credit: exactly one side carries a positive
/// amount. Unlike the budget ledger (whose side is fixed by entry type), a GL
/// line's side is chosen by the poster, so this is where the single-sided
/// invariant is actually checked.
/// </summary>
public sealed record GeneralLedgerEntry
{
    public DateOnly PostingDate { get; }
    public int FiscalYear { get; }

    /// <summary>RCA object account this line posts to.</summary>
    public string Account { get; }

    public Money Debit { get; }
    public Money Credit { get; }

    public string VoucherDoctype { get; }
    public string VoucherNo { get; }
    public string? Remarks { get; }

    public GeneralLedgerEntry(
        DateOnly postingDate,
        int fiscalYear,
        string account,
        Money debit,
        Money credit,
        string voucherDoctype,
        string voucherNo,
        string? remarks = null)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("GL account is required.", nameof(account));
        if (debit.IsNegative || credit.IsNegative)
            throw new SingleSidedViolationException("GL amounts cannot be negative; use the opposite side.");
        if (debit.IsPositive == credit.IsPositive)
            throw new SingleSidedViolationException(
                $"GL line must be debit XOR credit (got debit {debit}, credit {credit}).");

        PostingDate = postingDate;
        FiscalYear = fiscalYear;
        Account = account;
        Debit = debit;
        Credit = credit;
        VoucherDoctype = voucherDoctype;
        VoucherNo = voucherNo;
        Remarks = remarks;
    }

    public LedgerSide Side => Debit.IsPositive ? LedgerSide.Debit : LedgerSide.Credit;
}

/// <summary>
/// A set of GL lines that must post as one balanced journal. Enforces R-GL-01:
/// total debits equal total credits within a one-centavo tolerance. A batch that
/// will not balance cannot be posted.
/// </summary>
public sealed class GlPostingBatch
{
    private readonly List<GeneralLedgerEntry> _entries = [];

    public IReadOnlyList<GeneralLedgerEntry> Entries => _entries;

    public Money TotalDebit => _entries.Aggregate(Money.Zero, (sum, e) => sum + e.Debit);
    public Money TotalCredit => _entries.Aggregate(Money.Zero, (sum, e) => sum + e.Credit);

    public bool IsBalanced => TotalDebit.EqualsWithinTolerance(TotalCredit);

    public GlPostingBatch Add(GeneralLedgerEntry entry)
    {
        _entries.Add(entry);
        return this;
    }

    /// <summary>Throws unless the batch balances; call before persisting.</summary>
    public void EnsureBalanced()
    {
        if (!IsBalanced)
            throw new UnbalancedBatchException(
                $"Journal does not balance: debits {TotalDebit} ≠ credits {TotalCredit}.");
    }
}
