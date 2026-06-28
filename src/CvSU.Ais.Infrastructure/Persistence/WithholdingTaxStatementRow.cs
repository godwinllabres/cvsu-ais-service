namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Withholding Tax Statement header. Lines are stored separately in
/// <see cref="WithholdingTaxLineRow"/>.</summary>
public sealed class WithholdingTaxStatementRow
{
    public string Name { get; set; } = default!;
    public string StatementType { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public string TaxPeriodMonth { get; set; } = default!;
    public string? FundCluster { get; set; }
    public string FundingSourceCode { get; set; } = default!;
    public string? PayeeName { get; set; }
    public string? PayeeTin { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? GlPostingReference { get; set; }
    public string? Remarks { get; set; }
}
