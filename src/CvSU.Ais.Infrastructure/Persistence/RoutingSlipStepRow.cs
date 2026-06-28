namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>A single step instance on a routing slip.</summary>
public sealed class RoutingSlipStepRow
{
    public int Id { get; set; }
    public string ParentSlipName { get; set; } = default!;
    public int StepOrder { get; set; }
    public string OfficeName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public string? HandledBy { get; set; }
    public string? Remarks { get; set; }

    public RoutingSlipRow Parent { get; set; } = default!;
}
