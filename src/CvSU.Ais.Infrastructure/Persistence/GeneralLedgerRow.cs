namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Accrual GL line as stored. <see cref="LedgerSeq"/> is a DB identity
/// column giving monotonic audit ordering. Append-only (see the marker).</summary>
public sealed class GeneralLedgerRow : IImmutableLedgerRow
{
    public long LedgerSeq { get; set; }
    public DateOnly PostingDate { get; set; }
    public int FiscalYear { get; set; }
    public string Account { get; set; } = default!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string VoucherDoctype { get; set; } = default!;
    public string VoucherNo { get; set; } = default!;
    public string? Remarks { get; set; }
}
