using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Budget-registry line as stored. The UACS tuple is flattened to
/// columns; <see cref="ExpenseClass"/> and <see cref="EntryType"/> persist as
/// readable strings. Append-only.</summary>
public sealed class BudgetLedgerRow : IImmutableLedgerRow
{
    public long LedgerSeq { get; set; }
    public DateOnly PostingDate { get; set; }
    public int FiscalYear { get; set; }

    public string FundingSourceCode { get; set; } = default!;
    public string PapCode { get; set; } = default!;
    public string LocationCode { get; set; } = default!;
    public ExpenseClass ExpenseClass { get; set; }
    public string ObjectAccountCode { get; set; } = default!;

    public BudgetEntryType EntryType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public string VoucherDoctype { get; set; } = default!;
    public string VoucherNo { get; set; } = default!;
    public string? AppropriationId { get; set; }
    public string? AllotmentId { get; set; }
}
