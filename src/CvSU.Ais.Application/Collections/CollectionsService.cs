using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Collections;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Collections;

/// <summary>
/// Records collections (Official Receipts). The defining concern is offline resilience: a cashier
/// must be able to receive cash during a Frappe/server outage, so the create path is
/// idempotency-keyed — a receipt captured offline and replayed on reconnect resolves to its
/// original OR number instead of creating a duplicate receipt or double-posting the GL.
///
/// On a fresh receipt the service, in ONE transaction: dedups on the idempotency key, resolves the
/// credit account from (fee type, fund cluster), mints the gapless OR number (server-authoritative),
/// issues + posts the balanced collection journal (DR cash / CR income-or-trust-liability), and
/// stores the receipt with its key. The credit-account resolution lives here at the service seam,
/// not in the domain — mirroring how the DV resolves its accounts at the application boundary.
/// </summary>
public sealed class CollectionsService(
    IReceiptStore receipts,
    IGeneralLedger generalLedger,
    IVoucherNumberGenerator numbers,
    IUnitOfWork unitOfWork)
{
    /// <summary>Fund cluster 07 = Trust Receipts (CLAUDE.md §4A.2).</summary>
    private const string TrustFundCluster = "07";

    /// <summary>Resolve the GL credit account for a collection from what is being collected and the
    /// fund cluster. Trust money (Fund 07, or fiduciary / student-org fees) credits a LIABILITY and
    /// is never recognised as income; own-source revenue credits the matching income account.
    /// The Fund-07 cluster overrides the fee type — anything in the trust fund is a trust liability.</summary>
    public static string ResolveCreditAccount(FeeType feeType, string fundClusterCode)
    {
        if (fundClusterCode?.Trim() == TrustFundCluster)
            return GlAccounts.TrustLiabilities;

        return feeType switch
        {
            FeeType.Tuition => GlAccounts.TuitionFeesIncome,
            FeeType.Fiduciary => GlAccounts.TrustLiabilities,
            FeeType.StudentOrg => GlAccounts.TrustLiabilities,
            FeeType.Other => GlAccounts.OtherServiceIncome,
            _ => throw new ArgumentOutOfRangeException(nameof(feeType), feeType, "Unknown fee type."),
        };
    }

    public Task<ReceiptView> RecordReceiptAsync(
        RecordReceiptRequest request, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new ArgumentException("An idempotency key is required to record a receipt.");

            // Replay safety: a key we've already issued returns the original receipt, no new row,
            // no second posting. This is what makes the offline outbox safe to retry.
            var existing = await receipts.FindByIdempotencyKeyAsync(request.IdempotencyKey, token);
            if (existing is not null)
            {
                var prior = (await receipts.ListAsync(token)).First(r => r.OrNumber == existing);
                return ToView(prior);
            }

            var mode = ParseMode(request.Mode);
            var feeType = ParseFeeType(request.FeeType);
            var creditAccount = ResolveCreditAccount(feeType, request.FundCluster);
            var receipt = new OfficialReceipt(
                request.Payor, new Money(request.AmountPaid), mode, feeType, request.FundCluster,
                creditAccount, request.ReceivedAtUtc, request.PaidToAccount, request.CostCenter);

            // Server-authoritative gapless OR number (AOR-YYYY-#####), assigned at issue/sync.
            var year = request.ReceivedAtUtc.Year;
            var orNumber = await numbers.NextAsync($"AOR-{year}", token);
            receipt.Issue(orNumber, DateTimeOffset.UtcNow);

            // Post the balanced collection journal through the immutable ledger core.
            await generalLedger.AppendBatchAsync(
                receipt.BuildCollectionPosting(DateOnly.FromDateTime(request.ReceivedAtUtc.UtcDateTime)), token);

            await receipts.AppendAsync(receipt, request.IdempotencyKey, token);

            return ToView(receipt);
        }, cancellationToken);

    public async Task<IReadOnlyList<ReceiptView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await receipts.ListAsync(cancellationToken);
        return rows.Select(ToView).ToList();
    }

    private static PaymentMode ParseMode(string mode) =>
        Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out var m)
            ? m
            : throw new ArgumentException($"Unknown payment mode '{mode}'.");

    private static FeeType ParseFeeType(string feeType) =>
        Enum.TryParse<FeeType>(feeType, ignoreCase: true, out var f)
            ? f
            : throw new ArgumentException($"Unknown fee type '{feeType}'.");

    private static ReceiptView ToView(OfficialReceipt r) => new(
        r.OrNumber!, r.Payor, r.AmountPaid.Amount, r.Mode.ToString(), r.FeeType.ToString(),
        r.FundCluster, r.PaidToAccount, r.CreditAccount, r.ReceivedAt, r.IssuedAt, r.Status.ToString());
}
