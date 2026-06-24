namespace CvSU.Ais.Application.Abstractions;

/// <summary>
/// Issues gapless, sequential voucher numbers. Government vouchers are legally
/// expected to be gapless, so this is deliberately <em>not</em> a bare database
/// sequence — a rolled-back transaction would burn a number and leave a gap.
/// Implementations increment a counter inside the caller's transaction so a
/// rollback un-burns the number.
/// </summary>
public interface IVoucherNumberGenerator
{
    /// <summary>
    /// Returns the next number for <paramref name="series"/> (e.g. "DV-2026"),
    /// formatted as "{series}-{counter:D5}". Must be called within an ambient
    /// database transaction that also persists the voucher, so the counter and
    /// the voucher commit or roll back together.
    /// </summary>
    Task<string> NextAsync(string series, CancellationToken cancellationToken = default);
}
