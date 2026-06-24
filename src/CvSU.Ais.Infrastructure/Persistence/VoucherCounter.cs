namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One row per naming series, holding the last-issued counter value.
/// Backs the gapless numbering service — incremented under an advisory lock
/// inside the caller's transaction.</summary>
public sealed class VoucherCounter
{
    public string Series { get; set; } = default!;
    public long Current { get; set; }
}
