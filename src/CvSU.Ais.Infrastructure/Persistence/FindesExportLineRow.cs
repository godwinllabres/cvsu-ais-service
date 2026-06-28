namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One DV reference included in a FINDES export batch.</summary>
public sealed class FindesExportLineRow
{
    public int Id { get; set; }
    public string ParentExportName { get; set; } = default!;
    public string DvReference { get; set; } = default!;

    public FindesExportRow Parent { get; set; } = default!;
}
