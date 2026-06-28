namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>BIR Form 2307 (Certificate of Creditable Withholding Tax at Source)
/// as stored. One row per certificate; linked to a DV by reference.</summary>
public sealed class Bir2307Row
{
    public string Name { get; set; } = default!;
    public string DvReference { get; set; } = default!;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public string PayeeName { get; set; } = default!;
    public string PayeeTin { get; set; } = default!;
    public string? PayeeAddress { get; set; }
    public string IncomePaymentType { get; set; } = default!;
    public decimal GrossAmount { get; set; }
    public decimal EwtRate { get; set; }
    public decimal EwtAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? Remarks { get; set; }
}
