// Declared under the original CvSU.Ais.Domain.Disbursement namespace so re-homing it
// into the shared assembly is transparent to the domain (no using changes). This is the
// single source of truth for the DV workflow states — the server's DvStateMachine drives
// the transitions; the Blazor client renders them. One enum, server and client.
namespace CvSU.Ais.Domain.Disbursement;

/// <summary>The DV workflow states. Single source of truth shared by the API and the
/// Blazor web client.</summary>
public enum DvWorkflowStatus
{
    Draft,
    IaAuditRequired,
    Submitted,
    Approved,
    ApprovedForPayment,
    Posted,
    Released,
    Closed,
}

/// <summary>What the DV pays for. Drives type-specific encoding gates — a
/// <see cref="Suppliers"/> DV requires a Supply/Property Officer sign-off before
/// it can be approved.</summary>
public enum DvType
{
    Suppliers,
    Payroll,
    CashAdvance,
    Reimbursement,
    Others,
}

/// <summary>How the disbursement is settled. The payment identifier
/// (<c>PaymentReference</c>) is unique per method across all DVs.</summary>
public enum DvPaymentMethod
{
    /// <summary>Commercial / MDS cheque.</summary>
    Cheque,

    /// <summary>Advice to Debit Account (LDDAP-ADA).</summary>
    Ada,

    /// <summary>Direct bank/wire transfer.</summary>
    BankTransfer,
}

/// <summary>The certifications a DV must carry before approval. Each is asserted by
/// the officer responsible for it (not the encoder) — see the per-certification role
/// in the domain — and is recorded with the certifier's identity and timestamp.</summary>
public enum Certification
{
    /// <summary>Budget Office fund-sufficiency certification (Budget Officer).</summary>
    BudgetSufficiency,

    /// <summary>Internal Audit confirmation (Internal Auditor).</summary>
    InternalAudit,

    /// <summary>End-user acceptance of the goods/services (End User).</summary>
    EndUserAcceptance,

    /// <summary>Accountant Box D signature (Accountant).</summary>
    AccountantSignature,

    /// <summary>Supply/Property Officer inspection &amp; acceptance (Supply/Property Officer).</summary>
    SupplyPropertyInspection,
}
