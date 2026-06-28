namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>A routing slip header — a live instance of a routing template for a specific document.</summary>
public sealed class RoutingSlipRow
{
    public string Name { get; set; } = default!;
    public string RoutingTemplateName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ReferenceDoctype { get; set; }
    public string? ReferenceName { get; set; }
    public int CurrentStep { get; set; }
    public string? CurrentOffice { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }

    public List<RoutingSlipStepRow> Steps { get; set; } = [];
}
