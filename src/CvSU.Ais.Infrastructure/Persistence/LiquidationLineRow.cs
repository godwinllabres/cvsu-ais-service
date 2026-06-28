namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One expense line on a Liquidation Report.</summary>
public sealed class LiquidationLineRow
{
    public int Id { get; set; }
    public string ParentLrName { get; set; } = default!;
    public string ExpenseType { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? ReceiptReference { get; set; }
    public DateOnly? ReceiptDate { get; set; }
    public string? AccountCode { get; set; }

    public LiquidationReportRow Parent { get; set; } = default!;
}
