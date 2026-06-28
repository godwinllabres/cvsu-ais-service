namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Audit trail entry written whenever any document transitions from one
/// workflow state to another. Append-only in practice.</summary>
public sealed class StateHistoryRow
{
    public int Id { get; set; }
    public string ReferenceDoctype { get; set; } = default!;
    public string ReferenceName { get; set; } = default!;
    public string FromState { get; set; } = default!;
    public string ToState { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string ActingUser { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public string? Remarks { get; set; }
}
