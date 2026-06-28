namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>LDDAP-ADA header as stored. Mutable (it transitions through an approval workflow).
/// Enum-valued fields (ApprovalStatus) persist as readable strings.</summary>
public sealed class LddapAdaRow
{
    public string Name { get; set; } = default!;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public string? FundCluster { get; set; }
    public string BankName { get; set; } = default!;
    public string BankAccountNumber { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public int TotalPayees { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public DateOnly? TransmittedDate { get; set; }
    public string? Remarks { get; set; }
}
