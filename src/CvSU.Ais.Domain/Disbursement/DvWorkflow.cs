using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;
using static CvSU.Ais.Domain.Disbursement.DvAction;
using static CvSU.Ais.Domain.Disbursement.DvWorkflowStatus;

namespace CvSU.Ais.Domain.Disbursement;

/// <summary>Submit/cancel lifecycle — the explicit analogue of Frappe's docstatus 0/1/2.</summary>
public enum DvLifecycleState { Draft, Submitted, Cancelled }

// DvWorkflowStatus now lives in the shared CvSU.Ais.Contracts assembly (still under the
// CvSU.Ais.Domain.Disbursement namespace) so the API and the Blazor client share ONE enum
// definition. The `using static ... DvWorkflowStatus` above resolves it from Contracts.

/// <summary>The actions a caller may attempt; each maps to at most one edge per state.
/// Names mirror the legacy Frappe workflow actions: RequestIaAudit = "Route to IA",
/// Submit = "Submit to Accounting", ReturnToClerk = "Return to Clerk".</summary>
public enum DvAction
{
    RequestIaAudit,
    Submit,
    ReturnToClerk,
    Approve,
    ApproveForPayment,
    Post,
    Release,
    Close,
}

/// <summary>Canonical role names the transitions gate on.
///
/// These string VALUES are the single source of truth and MUST match the Frappe
/// <c>Role.name</c> spellings seeded by the legacy app (see ais-template
/// <c>accounting/accounting_information_system/constants/roles.py</c>). Any divergence
/// silently breaks authorization the moment a real identity provider feeds these role
/// names in — a caller would hold "AIS Accounting Approver" but the gate would test
/// for "Accountant" and reject (or, worse, the reverse). Rename the constant
/// IDENTIFIER freely; never let the string drift from the Frappe name.
///
/// One mapping is intentionally lossy: the Frappe DV workflow splits
/// <c>AIS Accounting Staff</c> (Route-to-IA, Post, Close) from
/// <c>AIS Accounting Approver</c> (Approve). This engine currently gates Approve,
/// Post and Close on <see cref="Accountant"/> = "AIS Accounting Approver". If the
/// Staff/Approver split matters here, add a distinct <c>AccountingStaff</c> constant
/// and re-point the Post/Close edges — do not blur the strings.</summary>
public static class DvRoles
{
    public const string Encoder = "AIS Accounting Encoder";
    public const string InternalAuditor = "AIS Internal Auditor";
    public const string Accountant = "AIS Accounting Approver";
    public const string HeadOfAgency = "AIS Head of Agency";
    public const string Treasury = "AIS Cashier Collector";

    /// <summary>The end-user / requesting office that accepts the goods or services.
    /// Frappe role: <c>AIS Requesting Unit</c>.</summary>
    public const string EndUser = "AIS Requesting Unit";

    /// <summary>The Supply/Property Officer who performs inspection and acceptance.
    /// No dedicated Frappe role exists yet; kept distinct so the certification gate is
    /// explicit. Align to a real Frappe role when one is seeded.</summary>
    public const string SupplyPropertyOfficer = "AIS Supplies Officer";
}

/// <summary>One certification's audit state: done, and (for the trail) by whom and when.</summary>
public readonly record struct CertificationState(bool Done, string? By, DateTime? At)
{
    public static readonly CertificationState NotDone = new(false, null, null);
}

/// <summary>Maps each certification to the role responsible for asserting it. This is the
/// control that stops the encoder from self-certifying another office's sign-off.</summary>
public static class Certifications
{
    public static string RequiredRole(Certification certification) => certification switch
    {
        Certification.BudgetSufficiency => BudgetRoles.BudgetOfficer,
        Certification.InternalAudit => DvRoles.InternalAuditor,
        Certification.EndUserAcceptance => DvRoles.EndUser,
        Certification.AccountantSignature => DvRoles.Accountant,
        Certification.SupplyPropertyInspection => DvRoles.SupplyPropertyOfficer,
        _ => throw new ArgumentOutOfRangeException(nameof(certification), certification, "Unknown certification."),
    };
}

/// <summary>Who is acting and what roles they hold. A principal holding an
/// <see cref="AdminRoles">administrator role</see> is exempt from SoD (the documented
/// escape hatch), mirroring the legacy Frappe <c>!= "Administrator"</c> guard.
///
/// SECURITY: the bypass is keyed on a held ROLE, never on the username. The legacy
/// service treated any caller whose <c>X-User</c> was literally "Administrator" as
/// admin — a trivial SoD bypass, since the dev header scheme lets a caller assert any
/// username. Frappe grants the escape hatch to the bootstrap <c>Administrator</c> user
/// and the <c>System Manager</c> role; both are issued here as role claims by the
/// identity provider, so claiming a username no longer elevates anyone.</summary>
public sealed class TransitionContext(string actingUser, params string[] roles)
{
    /// <summary>Roles whose holder is exempt from SoD. These are Frappe-core role names
    /// (see ais-template <c>constants/roles.py</c>: <c>ROLE_ADMINISTRATOR</c>,
    /// <c>ROLE_SYSTEM_MANAGER</c>) and must stay in step with them.</summary>
    public static readonly string[] AdminRoles = ["Administrator", "System Manager"];

    private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

    public string ActingUser { get; } = actingUser;

    public bool IsAdministrator => AdminRoles.Any(_roles.Contains);

    public bool HasRole(string role) => IsAdministrator || _roles.Contains(role);
}

/// <summary>One edge of the state machine: the only way state changes.</summary>
public sealed record DvTransition(
    DvWorkflowStatus From,
    DvWorkflowStatus To,
    DvAction Action,
    string RequiredRole,
    Action<DisbursementVoucher, TransitionContext>? Guard = null,
    Action<DisbursementVoucher, TransitionContext>? OnApplied = null);

/// <summary>
/// The single DV engine. The legacy app drove <c>workflow_status</c> from two
/// places — the Frappe Workflow JSON and the Python controller — and the
/// whitelisted <c>set_workflow_status</c> saved with <c>ignore_permissions</c>,
/// leaving a role-free path. Here the table below is the <em>only</em> path, and
/// each edge carries its required role and guards. Drift and bypass are both
/// structurally impossible. The edge set is asserted equal to the legacy Frappe DV
/// workflow's transitions (the vendored <c>dv_workflow.json</c> drift test), so this
/// single engine reproduces the old two-engine behaviour exactly — no edge the legacy
/// system lacked, none it had dropped.
/// </summary>
public static class DvStateMachine
{
    public static IReadOnlyList<DvTransition> Transitions { get; } =
    [
        // Clerk lodges the DV and routes it to Internal Audit ("Route to IA"). The
        // encoding-complete gate fires here: nothing incomplete reaches Internal Audit.
        new(Draft, IaAuditRequired, RequestIaAudit, DvRoles.Encoder, Guards.EncodingComplete),
        // Internal Audit either forwards it to Accounting ("Submit to Accounting")...
        new(IaAuditRequired, Submitted, Submit, DvRoles.InternalAuditor, Guards.RequireAmount),
        // ...or bounces it back to the clerk ("Return to Clerk").
        new(IaAuditRequired, Draft, ReturnToClerk, DvRoles.InternalAuditor),
        // Accounting approval — the docstatus 0→1 boundary (SoD: approver ≠ encoder).
        new(Submitted, Approved, Approve, DvRoles.Accountant, Guards.ApproveGuard, Effects.RecordApprover),
        // Head of Agency authorises payment (SoD: ≠ encoder and ≠ approving accountant).
        new(Approved, ApprovedForPayment, ApproveForPayment, DvRoles.HeadOfAgency, Guards.ApproveForPaymentGuard, Effects.RecordPaymentApprover),
        // Accrual GL posting fires on Post; cash GL + budget Disbursement fire on Release.
        new(ApprovedForPayment, Posted, Post, DvRoles.Accountant, Guards.PostGuard),
        new(Posted, Released, Release, DvRoles.Treasury),
        new(Released, Closed, Close, DvRoles.Accountant),
    ];

    public static DvTransition Resolve(DvWorkflowStatus from, DvAction action)
    {
        var match = Transitions.FirstOrDefault(t => t.From == from && t.Action == action);
        if (match is null)
            throw new InvalidTransitionException(
                $"'{action}' is not a legal transition from '{from}'. " +
                $"Legal actions here: {string.Join(", ", LegalActions(from))}.");
        return match;
    }

    public static IEnumerable<DvAction> LegalActions(DvWorkflowStatus from) =>
        Transitions.Where(t => t.From == from).Select(t => t.Action);
}

/// <summary>Transition guards: SoD and certification gates, enforced regardless of caller.</summary>
internal static class Guards
{
    public static void RequireAmount(DisbursementVoucher dv, TransitionContext ctx)
    {
        if (!dv.Amount.IsPositive)
            throw new InvalidTransitionException("DV amount must be greater than zero (R-DV-01).");
    }

    /// <summary>The encoding-complete gate (R-DV-02): a DV may not leave Draft for
    /// Internal Audit until the encoder has supplied a positive amount and a complete
    /// UACS budget line, so nothing half-encoded reaches the rest of the workflow.</summary>
    public static void EncodingComplete(DisbursementVoucher dv, TransitionContext ctx)
    {
        RequireAmount(dv, ctx);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(dv.PapCode)) missing.Add("PAP code");
        if (string.IsNullOrWhiteSpace(dv.LocationCode)) missing.Add("location code");
        if (dv.ExpenseClass is null) missing.Add("expense class");
        if (string.IsNullOrWhiteSpace(dv.ObjectAccountCode)) missing.Add("object account code");
        if (missing.Count > 0)
            throw new InvalidTransitionException(
                "DV cannot be routed for audit — encoding is incomplete; supply the UACS budget line: " +
                $"{string.Join(", ", missing)} (R-DV-02).");
    }

    public static void ApproveGuard(DisbursementVoucher dv, TransitionContext ctx)
    {
        if (!ctx.IsAdministrator && SameUser(ctx.ActingUser, dv.Encoder))
            throw new SegregationOfDutiesException(
                "Segregation of Duties: the encoder cannot approve their own DV (R-DV-09). " +
                "Route the DV to a different accountant for approval.");
        EnsureCertified(dv);
        EnsureTaxWithinGross(dv);
        EnsureSupplyPropertySignedOff(dv);
    }

    /// <summary>Suppliers DVs require the Supply/Property Officer inspection-and-
    /// acceptance sign-off before approval (R-DV-07); other DV types do not.</summary>
    private static void EnsureSupplyPropertySignedOff(DisbursementVoucher dv)
    {
        if (dv.DvType == DvType.Suppliers && !dv.SupplyPropertySignedOff)
            throw new InvalidTransitionException(
                "Suppliers DV cannot be approved: the Supply/Property Officer inspection-and-acceptance " +
                "sign-off is missing (R-DV-07). Secure the Supply/Property sign-off before approval.");
    }

    private static void EnsureTaxWithinGross(DisbursementVoucher dv)
    {
        if (dv.TaxWithheld.IsNegative)
            throw new InvalidTransitionException("Tax withheld cannot be negative. Correct the tax figure.");
        if (dv.TaxWithheld > dv.Amount)
            throw new InvalidTransitionException(
                $"Tax withheld ({dv.TaxWithheld}) cannot exceed the gross amount ({dv.Amount}). " +
                "Correct the tax figure so the net payable is not negative.");
    }

    public static void ApproveForPaymentGuard(DisbursementVoucher dv, TransitionContext ctx)
    {
        if (ctx.IsAdministrator) return;
        if (SameUser(ctx.ActingUser, dv.Encoder))
            throw new SegregationOfDutiesException(
                "Segregation of Duties: the Head of Agency approving payment cannot be the DV encoder (R-DV-09).");
        if (SameUser(ctx.ActingUser, dv.ApprovedBy))
            throw new SegregationOfDutiesException(
                "Segregation of Duties: the Head of Agency must differ from the approving accountant (R-DV-09).");
    }

    public static void PostGuard(DisbursementVoucher dv, TransitionContext ctx)
    {
        EnsureCertified(dv);
        if (!dv.AccountantSigned)
            throw new InvalidTransitionException(
                "DV cannot be posted: the accountant signature (Box D) is missing (R-DV-06).");
    }

    private static void EnsureCertified(DisbursementVoucher dv)
    {
        var missing = new List<string>();
        if (!dv.BudgetCertified) missing.Add("Budget Office fund-sufficiency certification");
        if (!dv.InternalAuditConfirmed) missing.Add("Internal Audit confirmation");
        if (!dv.EndUserConfirmed) missing.Add("End-user acceptance confirmation");
        if (missing.Count > 0)
            throw new InvalidTransitionException(
                $"DV cannot advance — missing certifications: {string.Join(", ", missing)} (R-DV-05). " +
                "Secure the listed certifications before approval.");
    }

    private static bool SameUser(string a, string? b) =>
        b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Post-transition bookkeeping (who approved, when).</summary>
internal static class Effects
{
    public static void RecordApprover(DisbursementVoucher dv, TransitionContext ctx) =>
        dv.RecordApprovedBy(ctx.ActingUser);

    public static void RecordPaymentApprover(DisbursementVoucher dv, TransitionContext ctx) =>
        dv.RecordApprovedForPaymentBy(ctx.ActingUser);
}
