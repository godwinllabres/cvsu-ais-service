namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One DV line item on a DV Transmittal. References the parent transmittal header by name.</summary>
public sealed class DvTransmittalItemRow
{
    public int Id { get; set; }
    public string ParentTransmittalName { get; set; } = default!;
    public string DvReference { get; set; } = default!;
    public decimal DvAmount { get; set; }
    public string? Remarks { get; set; }
}
