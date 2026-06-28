namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One tax-class breakdown line inside a Withholding Tax Statement.</summary>
public sealed class WithholdingTaxLineRow
{
    public int Id { get; set; }
    public string ParentWhtName { get; set; } = default!;
    public string TaxType { get; set; } = default!;
    public string? TaxClass { get; set; }
    public string? AtcCode { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxBase { get; set; }
    public decimal TaxAmount { get; set; }
    public string? LiabilityAccount { get; set; }
    public string? SourceDv { get; set; }
    public string? Remarks { get; set; }
}
