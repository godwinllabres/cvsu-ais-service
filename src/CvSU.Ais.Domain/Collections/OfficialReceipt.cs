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

/// <summary>What is being collected. Mirrors the Frappe AIS Order of Payment fee_type options.
/// Drives the credit account: Tuition/Other → income; Fiduciary/StudentOrg → trust liability
/// (and a Fund-07 cluster forces the trust-liability credit regardless of fee type).</summary>
public enum FeeType
{
    Tuition,
    Fiduciary,
    StudentOrg,
    Other,
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
    public FeeType FeeType { get; }
    public string FundCluster { get; }

    /// <summary>RCA cash/bank account debited on issue. Defaults to Cash – Collecting Officers.</summary>
    public string PaidToAccount { get; }

    /// <summary>The income or trust-liability RCA account credited on issue, resolved from
    /// (fee type, fund cluster) by the application service — the domain doesn't do catalog lookups
    /// (same seam as the DV resolving its UACS at the service boundary). This is NOT a UACS object
    /// code and carries no expense_class: revenue/trust is a different classification from spend.</summary>
    public string CreditAccount { get; }

    /// <summary>Responsibility/cost center, if captured. Recorded on the receipt; carried to the GL
    /// once GeneralLedgerEntry gains a cost-center dimension (POC: stored on the receipt only).</summary>
    public string? CostCenter { get; }

    /// <summary>When cash actually changed hands (stamped at the window, even offline).</summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>When the official OR number was assigned + GL posted (== ReceivedAt when online).</summary>
    public DateTimeOffset? IssuedAt { get; private set; }

    public ReceiptStatus Status { get; private set; } = ReceiptStatus.Pending;

    public OfficialReceipt(
        string payor,
        Money amountPaid,
        PaymentMode mode,
        FeeType feeType,
        string fundCluster,
        string creditAccount,
        DateTimeOffset receivedAt,
        string? paidToAccount = null,
        string? costCenter = null)
    {
        if (string.IsNullOrWhiteSpace(payor))
            throw new ArgumentException("Payor is required on an Official Receipt.", nameof(payor));
        if (!amountPaid.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amountPaid), "Amount paid must be greater than zero.");
        if (string.IsNullOrWhiteSpace(fundCluster))
            throw new ArgumentException("Fund cluster is required.", nameof(fundCluster));
        if (string.IsNullOrWhiteSpace(creditAccount))
            throw new ArgumentException(
                "A credit account (income or trust liability) is required — a collection must post to a real account, not a placeholder.",
                nameof(creditAccount));

        Payor = payor;
        AmountPaid = amountPaid;
        Mode = mode;
        FeeType = feeType;
        FundCluster = fundCluster;
        CreditAccount = creditAccount;
        CostCenter = costCenter;
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
    /// DR the cash account / CR the resolved income (own-source revenue) or trust-liability
    /// (Fund 07 / fiduciary) account, for the amount received. This is the accrual GL only — a
    /// collection is NOT an obligation/disbursement and never touches the budget registry
    /// (two-books rule, CLAUDE.md §4A.1).</summary>
    public GlPostingBatch BuildCollectionPosting(DateOnly postingDate)
    {
        if (Status != ReceiptStatus.Issued || OrNumber is null)
            throw new InvalidOperationException("Receipt must be issued (numbered) before it can post.");

        var fiscalYear = postingDate.Year;
        var batch = new GlPostingBatch()
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, PaidToAccount,
                AmountPaid, Money.Zero, DocType, OrNumber, "Collection received"))
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, CreditAccount,
                Money.Zero, AmountPaid, DocType, OrNumber, "Collection received"));

        batch.EnsureBalanced();
        return batch;
    }
}
