namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Liquidation Report header as stored. Mutable (it transitions).
/// Enum-valued fields persist as readable strings.</summary>
public sealed class LiquidationReportRow
{
    public string Name { get; set; } = default!;
    public string CashAdvanceName { get; set; } = default!;
    public string Employee { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public string? FundCluster { get; set; }
    public decimal TotalLiquidated { get; set; }
    public decimal AdvanceAmount { get; set; }
    public decimal RefundDue { get; set; }
    public decimal ReimbursementDue { get; set; }
    public string Status { get; set; } = default!;
    public string? GlPostingReference { get; set; }
    public string? Remarks { get; set; }

    public List<LiquidationLineRow> Lines { get; set; } = [];
}
