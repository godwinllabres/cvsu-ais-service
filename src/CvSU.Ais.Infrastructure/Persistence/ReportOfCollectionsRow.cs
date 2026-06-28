namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Report of Collections and Deposits header as stored. Status persists as a readable string.</summary>
public sealed class ReportOfCollectionsRow
{
    public string Name { get; set; } = default!;
    public DateOnly ReportDate { get; set; }
    public int FiscalYear { get; set; }
    public string? FundCluster { get; set; }
    public string CollectingOfficer { get; set; } = default!;
    public string DepositSlipNo { get; set; } = default!;
    public DateOnly DepositDate { get; set; }
    public string DepositoryBank { get; set; } = default!;
    public string? DepositAccountNumber { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalDeposited { get; set; }
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }

    public List<RcdLineRow> Lines { get; set; } = [];
}
