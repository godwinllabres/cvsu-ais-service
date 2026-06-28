namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>A single ordered stop in a routing template.</summary>
public sealed class RoutingTemplateStepRow
{
    public int Id { get; set; }
    public string ParentTemplateName { get; set; } = default!;
    public int StepOrder { get; set; }
    public string OfficeName { get; set; } = default!;
    public string? Role { get; set; }
    public int DurationDays { get; set; } = 1;
    public bool IsRequired { get; set; } = true;

    public RoutingTemplateRow Parent { get; set; } = default!;
}
