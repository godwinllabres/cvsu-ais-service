using CvSU.Ais.Domain.Disbursement; // DvWorkflowStatus (shared, same assembly)
using CvSU.Ais.Domain.Funds;        // ExpenseClass (shared, same assembly)

namespace CvSU.Ais.Contracts;

// Wire DTOs shared by the API and the Blazor client — one definition, no drift.
// Read models are records (immutable); request models are mutable classes so the
// Blazor forms can two-way bind and ASP.NET model binding can populate them.

// ── Disbursement Voucher ─────────────────────────────────────────────────────────

/// <summary>Thin list/row view of a DV's lifecycle state.</summary>
public sealed record DvStateView(
    string Name,
    string Lifecycle,
    DvWorkflowStatus Status,
    string FundCluster,
    string? ApprovedBy,
    string? ApprovedForPaymentBy,
    string? ControlNumber = null);

/// <summary>One certification's state: whether it's done, and (for the audit trail)
/// who certified it and when.</summary>
public sealed record CertificationView(bool Done, string? By, DateTime? At);

/// <summary>The full single-DV read model: everything needed to process it.</summary>
public sealed record DvDetailView(
    string Name,
    int FiscalYear,
    string Encoder,
    decimal Amount,
    string Lifecycle,
    DvWorkflowStatus Status,
    DvType DvType,
    string FundingSourceCode,
    string FundClusterCode,
    string FundClusterName,
    string? PapCode,
    string? LocationCode,
    ExpenseClass? ExpenseClass,
    string? ObjectAccountCode,
    decimal TaxWithheld,
    decimal NetAmountPayable,
    CertificationView Budget,
    CertificationView InternalAudit,
    CertificationView EndUser,
    CertificationView Accountant,
    CertificationView SupplyProperty,
    string? ApprovedBy,
    string? ApprovedForPaymentBy,
    string? ControlNumber,
    DvPaymentMethod? PaymentMethod,
    string? PaymentReference);

/// <summary>The create-DV request body. Mutable so the Blazor form binds to it and
/// ASP.NET model binding populates it; the encoder is supplied server-side from the
/// authenticated caller, never the body.</summary>
public sealed class DvCreateRequest
{
    public int FiscalYear { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxWithheld { get; set; }
    public DvType DvType { get; set; } = DvType.Suppliers;
    public string FundingSourceCode { get; set; } = "01101101";
    public string PapCode { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public ExpenseClass ExpenseClass { get; set; } = ExpenseClass.Mooe;
    public string ObjectAccountCode { get; set; } = "";

    // Certifications are NOT set here: each is asserted later by its responsible officer
    // via the certify action, so the encoder cannot self-certify another office's control.
}

/// <summary>Records how a DV is paid. The <see cref="Reference"/> (cheque no /
/// ADA no / transfer ref) is enforced unique per <see cref="Method"/> across all DVs.</summary>
public sealed class DvPaymentRequest
{
    public DvPaymentMethod Method { get; set; }
    public string Reference { get; set; } = "";
}

/// <summary>Asks to record a certification on a DV. The certifier is the authenticated
/// caller; the domain rejects the request unless the caller holds the role responsible
/// for <see cref="Certification"/>.</summary>
public sealed class DvCertifyRequest
{
    public Certification Certification { get; set; }
}

// ── Budget execution cycle ───────────────────────────────────────────────────────

public sealed record AppropriationView(
    string Id, decimal FinalAppropriation, decimal Allotted, decimal Unallotted);

public sealed record AllotmentView(
    string Id, string AppropriationId, decimal Amount, decimal Obligated, decimal Unobligated);

public sealed record ObligationView(
    string Id, string AllotmentId, decimal Amount, decimal AllotmentUnobligatedBalance);

/// <summary>Create-appropriation request body (mutable for Blazor form + model binding).</summary>
public sealed class AppropriationCreateRequest
{
    public int FiscalYear { get; set; }
    public string FundingSourceCode { get; set; } = "01101101";
    public string PapCode { get; set; } = "";
    public string LocationCode { get; set; } = "";
    public ExpenseClass ExpenseClass { get; set; } = ExpenseClass.Mooe;
    public string ObjectAccountCode { get; set; } = "";
    public decimal FinalAppropriation { get; set; }
}

/// <summary>An amount-only request body (allot / obligate).</summary>
public sealed record AmountRequest(decimal Amount);

// ── Collections (Official Receipt) ───────────────────────────────────────────────

/// <summary>Record-a-collection request. The cashier captures this at the window — possibly
/// offline. <c>IdempotencyKey</c> is client-generated and reused on replay so a queued receipt
/// that syncs more than once creates exactly one receipt + one GL posting. <c>ReceivedAtUtc</c>
/// is the real moment cash changed hands (preserved for audit even when synced later); the server
/// assigns the official OR number at issue/sync time, never the client.</summary>
public sealed class RecordReceiptRequest
{
    public string IdempotencyKey { get; set; } = "";
    public string Payor { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public string Mode { get; set; } = "Cash";
    /// <summary>What is being collected — drives the credit account server-side
    /// (Tuition/Other → income; Fiduciary/StudentOrg → trust liability). One of:
    /// Tuition, Fiduciary, StudentOrg, Other.</summary>
    public string FeeType { get; set; } = "Tuition";
    public string FundCluster { get; set; } = "01";
    public string? PaidToAccount { get; set; }
    public string? CostCenter { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}

/// <summary>An issued Official Receipt as returned to the client. <see cref="CreditAccount"/> is
/// the income/trust-liability account the server resolved and posted (display-only on the client;
/// the client must never compute it — see CLAUDE.md §5.1).</summary>
public sealed record ReceiptView(
    string OrNumber,
    string Payor,
    decimal AmountPaid,
    string Mode,
    string FeeType,
    string FundCluster,
    string PaidToAccount,
    string CreditAccount,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? IssuedAtUtc,
    string Status);

// ── Reports (COA registries) ──────────────────────────────────────────────────────
// Derived fields are computed properties (server and client compute them identically
// from the core values), so the JSON the API emits and the client renders never drift.

public sealed record BudgetRegistryRow(
    string FundClusterCode, string FundClusterName, RegistryType Registry, ExpenseClass ExpenseClass,
    decimal Allotment, decimal Obligation, decimal Disbursement)
{
    public decimal UnobligatedBalance => Allotment - Obligation;
    public decimal UnpaidObligation => Obligation - Disbursement;
}

public sealed record BudgetRegistryTotals(decimal Allotment, decimal Obligation, decimal Disbursement)
{
    public decimal UnobligatedBalance => Allotment - Obligation;
    public decimal UnpaidObligation => Obligation - Disbursement;
}

public sealed record BudgetRegistrySection(
    string Registry, IReadOnlyList<BudgetRegistryRow> Lines, BudgetRegistryTotals Totals);

public sealed record BudgetRegistryReport(
    int FiscalYear, BudgetRegistrySection Raod, BudgetRegistrySection Rbud);

public sealed record AppropriationAllotmentRow(
    string FundClusterCode, string FundClusterName, ExpenseClass ExpenseClass,
    decimal Appropriation, decimal Allotment)
{
    public decimal UnallottedBalance => Appropriation - Allotment;
}

public sealed record AppropriationAllotmentReport(
    int FiscalYear, IReadOnlyList<AppropriationAllotmentRow> Lines,
    decimal TotalAppropriation, decimal TotalAllotment)
{
    public decimal TotalUnallotted => TotalAppropriation - TotalAllotment;
}

public sealed record TrialBalanceRow(string Account, decimal Debit, decimal Credit);

public sealed record TrialBalanceReport(
    int FiscalYear, IReadOnlyList<TrialBalanceRow> Lines, decimal TotalDebit, decimal TotalCredit)
{
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}
