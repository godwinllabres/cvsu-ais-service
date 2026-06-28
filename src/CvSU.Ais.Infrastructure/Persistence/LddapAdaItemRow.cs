namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One payee line in a LDDAP-ADA. References the parent LDDAP-ADA header by name.</summary>
public sealed class LddapAdaItemRow
{
    public int Id { get; set; }
    public string ParentLddapName { get; set; } = default!;
    public string DvReference { get; set; } = default!;
    public string PayeeName { get; set; } = default!;
    public string? PayeeAccountNumber { get; set; }
    public string? BankName { get; set; }
    public decimal NetAmount { get; set; }
}
