namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Bank collection report header — one per LBP daily collection statement.</summary>
public sealed class BankCollectionReportRow
{
    public string Name { get; set; } = default!;
    public DateOnly ReportDate { get; set; }
    public string ReconciliationStatus { get; set; } = default!;
    public int TotalLines { get; set; }
    public decimal TotalAmount { get; set; }
    public int ExceptionsCount { get; set; }
    public string? Remarks { get; set; }

    public List<BankCollectionLineRow> Lines { get; set; } = [];
}
