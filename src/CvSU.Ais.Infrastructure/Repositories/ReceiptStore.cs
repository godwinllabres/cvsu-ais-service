using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Collections;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

/// <summary>Persists issued Official Receipts and resolves offline-replay idempotency. The unique
/// index on <c>idempotency_key</c> is the durable backstop: even if two replays race past the
/// lookup, the DB rejects the second insert rather than creating a duplicate receipt.</summary>
public sealed class ReceiptStore(AisDbContext db) : IReceiptStore
{
    public async Task<string?> FindByIdempotencyKeyAsync(
        string idempotencyKey, CancellationToken cancellationToken = default) =>
        await db.Set<OfficialReceiptRow>()
            .Where(r => r.IdempotencyKey == idempotencyKey)
            .Select(r => r.OrNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AppendAsync(
        OfficialReceipt receipt, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        db.Add(new OfficialReceiptRow
        {
            OrNumber = receipt.OrNumber!,
            IdempotencyKey = idempotencyKey,
            Payor = receipt.Payor,
            AmountPaid = receipt.AmountPaid.Amount,
            Mode = receipt.Mode.ToString(),
            FundCluster = receipt.FundCluster,
            PaidToAccount = receipt.PaidToAccount,
            ReceivedAt = receipt.ReceivedAt,
            IssuedAt = receipt.IssuedAt,
            Status = receipt.Status.ToString(),
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfficialReceipt>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Set<OfficialReceiptRow>()
            .OrderByDescending(r => r.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(Rehydrate).ToList();
    }

    private static OfficialReceipt Rehydrate(OfficialReceiptRow r)
    {
        var receipt = new OfficialReceipt(
            r.Payor, new Money(r.AmountPaid),
            Enum.Parse<PaymentMode>(r.Mode), r.FundCluster, r.ReceivedAt, r.PaidToAccount);
        receipt.Issue(r.OrNumber, r.IssuedAt ?? r.ReceivedAt);
        return receipt;
    }
}
