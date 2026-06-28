namespace CvSU.Ais.Infrastructure.Persistence;

public sealed class LocationCodeRow
{
    public string PsgcCode { get; set; } = default!;
    public string LocationName { get; set; } = default!;
    /// <summary>e.g. "Region", "Province", "City", "Municipality", "Barangay"</summary>
    public string Level { get; set; } = default!;
    public string? ParentCode { get; set; }
    public bool IsGroup { get; set; }
}
