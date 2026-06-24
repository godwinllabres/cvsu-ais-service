using CvSU.Ais.Domain.Common;
using static CvSU.Ais.Domain.Disbursement.DvAction;
using static CvSU.Ais.Domain.Disbursement.DvWorkflowStatus;

namespace CvSU.Ais.Domain.Disbursement;

/// <summary>Submit/cancel lifecycle — the explicit analogue of Frappe's docstatus 0/1/2.</summary>
public enum DvLifecycleState { Draft, Submitted, Cancelled }

/// <summary>The DV workflow states. Single source of truth — there is no parallel
/// JSON workflow definition that can drift from this enum.</summary>
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
    Rejected,
}

/// <summary>The actions a caller may attempt; each maps to at most one edge per state.</summary>
public enum DvAction
{
    RequestIaAudit,
    Submit,
    Approve,
    ApproveForPayment,
    Post,
    Release,
    Close,
    Reject,
}

/// <summary>Canonical role names the transitions gate on.</summary>
public static class DvRoles
{
    public const string Encoder = "Accounting Clerk";
    public const string Accountant = "Accountant";
    public const string HeadOfAgency = "Head of Agency";
    public const string Treasury = "Cashier";
}

/// <summary>Who is acting and what roles they hold. Administrator holds every role
/// and is exempt from SoD (the documented escape hatch), mirroring the legacy
/// <c>!= "Administrator"</c> guard.</summary>
public sealed class TransitionContext(string actingUser, params string[] roles)
{
    private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

    public string ActingUser { get; } = actingUser;

    public bool IsAdministrator =>
        string.Equals(ActingUser, "Administrator", StringComparison.OrdinalIgnoreCase);

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
/// structurally impossible.
/// </summary>
public static class DvStateMachine
{
    public static IReadOnlyList<DvTransition> Transitions { get; } =
    [
        new(Draft, IaAuditRequired, RequestIaAudit, DvRoles.Encoder, Guards.RequireAmount),
        new(Draft, Submitted, Submit, DvRoles.Encoder, Guards.RequireAmount),
        new(IaAuditRequired, Submitted, Submit, DvRoles.Encoder, Guards.RequireAmount),
        new(Submitted, Approved, Approve, DvRoles.Accountant, Guards.ApproveGuard, Effects.RecordApprover),
        new(Submitted, Rejected, Reject, DvRoles.Accountant),
        new(Approved, ApprovedForPayment, ApproveForPayment, DvRoles.HeadOfAgency, Guards.ApproveForPaymentGuard, Effects.RecordPaymentApprover),
        new(Approved, Rejected, Reject, DvRoles.HeadOfAgency),
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

    public static void ApproveGuard(DisbursementVoucher dv, TransitionContext ctx)
    {
        if (!ctx.IsAdministrator && SameUser(ctx.ActingUser, dv.Encoder))
            throw new SegregationOfDutiesException(
                "Segregation of Duties: the encoder cannot approve their own DV (R-DV-09). " +
                "Route the DV to a different accountant for approval.");
        EnsureCertified(dv);
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
