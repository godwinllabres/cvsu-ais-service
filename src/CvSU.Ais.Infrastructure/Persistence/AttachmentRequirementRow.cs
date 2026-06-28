namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Declares what attachment files are expected for a given doctype + workflow state.</summary>
public sealed class AttachmentRequirementRow
{
    public int Id { get; set; }
    public string ReferenceDoctype { get; set; } = default!;
    public string? WorkflowState { get; set; }
    public string RequirementCode { get; set; } = default!;
    public string RequirementLabel { get; set; } = default!;
    public string ValidationMode { get; set; } = default!;
    public string? FilenameKeyword { get; set; }
    public bool IsEnabled { get; set; } = true;
}
