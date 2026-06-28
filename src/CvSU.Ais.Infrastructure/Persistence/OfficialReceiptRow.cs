namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Official Receipt as stored. CollectionStatus persists as a readable string.</summary>
public sealed class OfficialReceiptRow
{
    public string Name { get; set; } = default!;
    public string OrNumber { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public string? OrderOfPaymentName { get; set; }
    public string Customer { get; set; } = default!;
    public decimal AmountPaid { get; set; }
    public string ModeOfPayment { get; set; } = default!;
    public string? FundCluster { get; set; }
    public string CollectionStatus { get; set; } = default!;
    public string? Remarks { get; set; }
}
