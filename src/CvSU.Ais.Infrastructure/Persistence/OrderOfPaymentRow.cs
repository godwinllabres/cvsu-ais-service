namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Order of Payment as stored. Status persists as a readable string.</summary>
public sealed class OrderOfPaymentRow
{
    public string Name { get; set; } = default!;
    public DateOnly OrderDate { get; set; }
    public string Customer { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? FundCluster { get; set; }
    public string Status { get; set; } = default!;
    public string? IssuedBy { get; set; }
    public string? Remarks { get; set; }
}
