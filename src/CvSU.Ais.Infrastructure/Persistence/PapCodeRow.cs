namespace CvSU.Ais.Infrastructure.Persistence;

public sealed class PapCodeRow
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? ParentCode { get; set; }
    public bool IsGroup { get; set; }
}
