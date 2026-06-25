using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Domain.Collections;

/// <summary>How the payment was tendered. Mirrors the AIS Official Receipt mode_of_payment.</summary>
public enum PaymentMode
{
    Cash,
    Check,
    BankTransfer,
    Online,
}

/// <summary>Where a receipt is in its short lifecycle. Collections are point-of-receipt events,
/// so the meaningful states are: captured offline but not yet given an official number (Pending),
/// and issued with a gapless OR number + posted to the GL (Issued).</summary>
public enum ReceiptStatus
{
    Pending,
    Issued,
    Cancelled,
}

/// <summary>
/// An Official Receipt — the record that money was received (fees, payments to the university).
/// Faithful to the Frappe AIS `AIS Official Receipt`: a payor, an amount, a mode of payment, a
/// fund cluster, and the cash account debited. Issuing the receipt posts a balanced GL journal
/// (DR cash / CR collections-clearing) and assigns the legally-gapless OR number.
///
/// OFFLINE CARVE-OUT (see SSO/collections design): unlike approvals — which must never be queued
/// because their timestamp is an audit-anchored consent event — a collection records cash that
/// physically changed hands at <see cref="ReceivedAt"/>. During a server outage the cashier
/// captures the receipt locally with that real timestamp; on reconnect it replays (idempotency
/// key) and the SERVER assigns the gapless <see cref="OrNumber"/> and posts the GL. Both
/// timestamps are preserved so COA can see when cash was received vs when the OR was issued.
/// </summary>
public sealed class OfficialReceipt
{
    public const string DocType = "AIS Official Receipt";

    /// <summary>The official, gapless receipt number (e.g. AOR-2026-00001). Null until issued —
    /// it is server-authoritative and assigned at issue/sync time, never minted offline.</summary>
    public string? OrNumber { get; private set; }

    public string Payor { get; }
    public Money AmountPaid { get; }
    public PaymentMode Mode { get; }
    public string FundCluster { get; }

    /// <summary>RCA cash/bank account debited on issue. Defaults to Cash – Collecting Officers.</summary>
    public string PaidToAccount { get; }

    /// <summary>When cash actually changed hands (stamped at the window, even offline).</summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>When the official OR number was assigned + GL posted (== ReceivedAt when online).</summary>
    public DateTimeOffset? IssuedAt { get; private set; }

    public ReceiptStatus Status { get; private set; } = ReceiptStatus.Pending;

    public OfficialReceipt(
        string payor,
        Money amountPaid,
        PaymentMode mode,
        string fundCluster,
        DateTimeOffset receivedAt,
        string? paidToAccount = null)
    {
        if (string.IsNullOrWhiteSpace(payor))
            throw new ArgumentException("Payor is required on an Official Receipt.", nameof(payor));
        if (!amountPaid.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amountPaid), "Amount paid must be greater than zero.");
        if (string.IsNullOrWhiteSpace(fundCluster))
            throw new ArgumentException("Fund cluster is required.", nameof(fundCluster));

        Payor = payor;
        AmountPaid = amountPaid;
        Mode = mode;
        FundCluster = fundCluster;
        ReceivedAt = receivedAt;
        PaidToAccount = string.IsNullOrWhiteSpace(paidToAccount)
            ? GlAccounts.CashCollectingOfficers
            : paidToAccount!;
    }

    /// <summary>Assign the server-authoritative gapless OR number and mark issued. Called once,
    /// at issue (online) or sync (after an offline capture). The number comes from the gapless
    /// sequence service — this aggregate never invents it.</summary>
    public void Issue(string orNumber, DateTimeOffset issuedAt)
    {
        if (Status == ReceiptStatus.Issued)
            throw new InvalidOperationException($"Receipt {OrNumber} is already issued.");
        if (string.IsNullOrWhiteSpace(orNumber))
            throw new ArgumentException("An official OR number is required to issue.", nameof(orNumber));

        OrNumber = orNumber;
        IssuedAt = issuedAt;
        Status = ReceiptStatus.Issued;
    }

    /// <summary>The balanced GL journal posted when the receipt is issued:
    /// DR the cash account / CR collections-clearing, for the amount received.</summary>
    public GlPostingBatch BuildCollectionPosting(DateOnly postingDate)
    {
        if (Status != ReceiptStatus.Issued || OrNumber is null)
            throw new InvalidOperationException("Receipt must be issued (numbered) before it can post.");

        var fiscalYear = postingDate.Year;
        var batch = new GlPostingBatch()
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, PaidToAccount,
                AmountPaid, Money.Zero, DocType, OrNumber, "Collection received"))
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, GlAccounts.CollectionsClearing,
                Money.Zero, AmountPaid, DocType, OrNumber, "Collection received"));

        batch.EnsureBalanced();
        return batch;
    }
}
