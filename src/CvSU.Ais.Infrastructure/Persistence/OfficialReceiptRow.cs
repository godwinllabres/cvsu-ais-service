namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Persisted Official Receipt. The <see cref="IdempotencyKey"/> is unique — it both backs
/// offline-replay dedup and is the row's natural client correlation id. The OR number is the
/// server-assigned gapless identity (unique too).</summary>
public sealed class OfficialReceiptRow
{
    public long Id { get; set; }
    public string OrNumber { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string Payor { get; set; } = default!;
    public decimal AmountPaid { get; set; }
    public string Mode { get; set; } = default!;
    public string FundCluster { get; set; } = default!;
    public string PaidToAccount { get; set; } = default!;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public string Status { get; set; } = default!;
}
