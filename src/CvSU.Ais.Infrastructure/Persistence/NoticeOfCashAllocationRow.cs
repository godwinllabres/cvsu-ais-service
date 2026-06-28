namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>NCA row. Status is persisted as a readable string.
/// UtilizedAmount is updated as DVs are released against this NCA.</summary>
public sealed class NoticeOfCashAllocationRow
{
    public string NcaNumber { get; set; } = default!;
    public DateOnly DateReceived { get; set; }
    public int FiscalYear { get; set; }
    public string FundingSourceCode { get; set; } = default!;
    public DateOnly ValidityDate { get; set; }
    public string Status { get; set; } = default!;
    public decimal NcaAmount { get; set; }
    public decimal UtilizedAmount { get; set; }
    public string? Remarks { get; set; }
}
