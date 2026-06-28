namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Cash Advance as stored. Mutable (it transitions through statuses).
/// Enum-valued fields persist as readable strings.</summary>
public sealed class CashAdvanceRow
{
    public string Name { get; set; } = default!;
    public string Employee { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public string? FundCluster { get; set; }
    public string Purpose { get; set; } = default!;
    public decimal AdvanceAmount { get; set; }
    public decimal LiquidatedAmount { get; set; }
    public decimal UnliquidatedBalance { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = default!;
    public string? GlPostingReference { get; set; }
    public string? Remarks { get; set; }
}
