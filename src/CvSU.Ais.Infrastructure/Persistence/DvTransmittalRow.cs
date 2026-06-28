namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>DV Transmittal header as stored. Mutable (it progresses from Draft through to Completed).
/// Enum-valued field (Status) persists as a readable string.</summary>
public sealed class DvTransmittalRow
{
    public string Name { get; set; } = default!;
    public DateOnly TransmittalDate { get; set; }
    public string TransmittingOfficer { get; set; } = default!;
    public string ReceivingCashier { get; set; } = default!;
    public string? AccountantName { get; set; }
    public bool AccountantSignatureConfirmed { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalDvCount { get; set; }
    public string Status { get; set; } = default!;
    public string? ReceivedByCashier { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public string? Remarks { get; set; }
}
