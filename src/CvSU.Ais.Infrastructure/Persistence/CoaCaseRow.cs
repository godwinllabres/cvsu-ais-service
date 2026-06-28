namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>COA audit case as stored. Mutable — the case transitions through
/// audit workflow states until settled and submitted to COA.</summary>
public sealed class CoaCaseRow
{
    public string Name { get; set; } = default!;
    public string NdNcReference { get; set; } = default!;
    public string? NfdReference { get; set; }
    public string? CoeReference { get; set; }
    public string LiableParty { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? SettlementMode { get; set; }
    public string? OrReference { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
}
