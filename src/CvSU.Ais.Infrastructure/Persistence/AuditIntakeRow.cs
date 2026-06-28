namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Audit Intake record as stored. Tracks the journey of a DV through the
/// internal audit unit. Enum-valued fields (AuditResult, Status) persist as readable strings.</summary>
public sealed class AuditIntakeRow
{
    public string Name { get; set; } = default!;
    public string DisbursementVoucherName { get; set; } = default!;
    public DateTime? ReceivedTimestamp { get; set; }
    public DateTime? RecordedTimestamp { get; set; }
    public string AuditResult { get; set; } = default!;
    public string? Findings { get; set; }
    public DateTime? ReleasedTimestamp { get; set; }
    public string? ReleasedTo { get; set; }
    public string Status { get; set; } = default!;
}
