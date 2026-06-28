namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>FINDES export batch header — groups the DVs selected for a single bank payment run.</summary>
public sealed class FindesExportRow
{
    public string Name { get; set; } = default!;
    public string? ExportBatch { get; set; }
    public DateOnly ExportDate { get; set; }
    public decimal DvTotalAmount { get; set; }
    public decimal ExportTotalAmount { get; set; }
    public decimal Variance { get; set; }
    public bool VarianceAcceptable { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? GeneratedBy { get; set; }
    public DateTime? GeneratedOn { get; set; }
    public string? Remarks { get; set; }

    public List<FindesExportLineRow> Lines { get; set; } = [];
}
