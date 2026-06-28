namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One collection line in a bank collection report — a single OR/receipt from the LBP statement.</summary>
public sealed class BankCollectionLineRow
{
    public int Id { get; set; }
    public string ParentReportName { get; set; } = default!;
    public string RefNo { get; set; } = default!;
    public string? LbpRefNo { get; set; }
    public decimal Amount { get; set; }
    public bool IsMatched { get; set; }
    public string? MatchedOrName { get; set; }
    public string? Remarks { get; set; }

    public BankCollectionReportRow Parent { get; set; } = default!;
}
