namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Journal Entry header as stored. Enum-valued fields persist as readable strings.</summary>
public sealed class JournalEntryRow
{
    public string Name { get; set; } = default!;
    public string? Title { get; set; }
    public DateOnly PostingDate { get; set; }
    public int FiscalYear { get; set; }
    public string? FundCluster { get; set; }
    public string JeType { get; set; } = default!;
    public string ApprovalStatus { get; set; } = default!;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? GlPostingReference { get; set; }
    public string? UserRemark { get; set; }

    public List<JeLineRow> JeLines { get; set; } = [];
}
