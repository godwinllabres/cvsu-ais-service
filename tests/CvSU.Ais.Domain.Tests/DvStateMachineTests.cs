using System.Text.Json;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;
using Xunit;

namespace CvSU.Ais.Domain.Tests;

/// <summary>
/// Defect #1: one state machine, role-gated on every edge, guards enforced
/// against direct command invocation (not just the UI). These tests stand in for
/// the legacy <c>set_workflow_status</c> bypass that saved with
/// <c>ignore_permissions</c>.
/// </summary>
public class DvStateMachineTests
{
    private const string Clerk = "clerk@cvsu";
    private const string Auditor = "auditor@cvsu";
    private static readonly DateTime CertTime = new(2026, 6, 25, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>A context holding an admin role — the SoD escape hatch is keyed on the
    /// role, not the username (see TransitionContext.AdminRoles).</summary>
    private static TransitionContext Admin(string user = "admin@cvsu") =>
        new(user, "System Manager");

    /// <summary>A Draft with a complete UACS budget line (so the encoding-complete gate
    /// passes) but no certifications yet.</summary>
    private static DisbursementVoucher DraftWithBudgetLine(string encoder = Clerk, DvType dvType = DvType.Suppliers) =>
        new("DV-2026-0001", encoder, new Money(1000m), TestData.RegularAgencyFund(), dvType)
        {
            PapCode = "100000100001000",
            LocationCode = "0102301000000",
            ExpenseClass = ExpenseClass.Mooe,
            ObjectAccountCode = "50203010",
        };

    /// <summary>Certify as the officer who holds the role responsible for that certification.</summary>
    private static void Certify(DisbursementVoucher dv, Certification cert, string user) =>
        dv.Certify(cert, new TransitionContext(user, Certifications.RequiredRole(cert)), CertTime);

    /// <summary>The four certifications every DV needs (excludes the Suppliers-only sign-off).</summary>
    private static void CertifyBase(DisbursementVoucher dv)
    {
        Certify(dv, Certification.BudgetSufficiency, "budget@cvsu");
        Certify(dv, Certification.InternalAudit, "auditor@cvsu");
        Certify(dv, Certification.EndUserAcceptance, "enduser@cvsu");
        Certify(dv, Certification.AccountantSignature, "boxd@cvsu");
    }

    private static DisbursementVoucher FullyCertifiedDraft(
        string encoder = Clerk, DvType dvType = DvType.Suppliers, bool supplyProperty = true)
    {
        var dv = DraftWithBudgetLine(encoder, dvType);
        CertifyBase(dv);
        if (supplyProperty) Certify(dv, Certification.SupplyPropertyInspection, "supply@cvsu");
        return dv;
    }

    /// <summary>The IA-first route to Accounting: the clerk routes to Internal Audit,
    /// then the auditor submits to Accounting. There is no direct Draft → Submitted edge.</summary>
    private static void RouteToAccounting(DisbursementVoucher dv)
    {
        dv.Fire(DvAction.RequestIaAudit, new TransitionContext(dv.Encoder, DvRoles.Encoder));
        dv.Fire(DvAction.Submit, new TransitionContext(Auditor, DvRoles.InternalAuditor));
    }

    [Fact]
    public void Happy_path_runs_draft_to_closed_with_the_right_roles()
    {
        var dv = FullyCertifiedDraft();

        dv.Fire(DvAction.RequestIaAudit, new TransitionContext(Clerk, DvRoles.Encoder));
        Assert.Equal(DvWorkflowStatus.IaAuditRequired, dv.Status);
        Assert.Equal(DvLifecycleState.Draft, dv.Lifecycle); // still doc_status 0

        dv.Fire(DvAction.Submit, new TransitionContext(Auditor, DvRoles.InternalAuditor));
        Assert.Equal(DvWorkflowStatus.Submitted, dv.Status);
        Assert.Equal(DvLifecycleState.Draft, dv.Lifecycle); // "Submitted" is still doc_status 0

        // Approval is the docstatus 0 → 1 boundary (mirrors the legacy submit==approval).
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Approved, dv.Status);
        Assert.Equal(DvLifecycleState.Submitted, dv.Lifecycle);
        Assert.Equal("accountant@cvsu", dv.ApprovedBy);

        dv.Fire(DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));
        Assert.Equal(DvWorkflowStatus.ApprovedForPayment, dv.Status);
        Assert.Equal("head@cvsu", dv.ApprovedForPaymentBy);

        dv.Fire(DvAction.Post, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Posted, dv.Status);

        dv.Fire(DvAction.Release, new TransitionContext("cashier@cvsu", DvRoles.Treasury));
        Assert.Equal(DvWorkflowStatus.Released, dv.Status);

        dv.Fire(DvAction.Close, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Closed, dv.Status);
    }

    [Fact]
    public void Illegal_edge_is_rejected()
    {
        var dv = FullyCertifiedDraft();
        RouteToAccounting(dv);

        // Post is only legal from ApprovedForPayment, never from Submitted.
        Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Post, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public void There_is_no_direct_draft_to_submitted_edge()
    {
        var dv = FullyCertifiedDraft();

        // The clerk cannot skip Internal Audit: the only edge out of Draft is Route to IA.
        Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Submit, new TransitionContext(Clerk, DvRoles.Encoder)));
        Assert.Equal(DvWorkflowStatus.Draft, dv.Status);
    }

    [Fact]
    public void Only_the_internal_auditor_may_submit_to_accounting()
    {
        var dv = FullyCertifiedDraft();
        dv.Fire(DvAction.RequestIaAudit, new TransitionContext(Clerk, DvRoles.Encoder));

        // The clerk who created it cannot also perform the IA "Submit to Accounting".
        var ex = Assert.Throws<UnauthorizedTransitionException>(
            () => dv.Fire(DvAction.Submit, new TransitionContext(Clerk, DvRoles.Encoder)));
        Assert.Equal("workflow.unauthorized", ex.Code);
        Assert.Equal(DvWorkflowStatus.IaAuditRequired, dv.Status); // unchanged

        // The Internal Auditor can.
        dv.Fire(DvAction.Submit, new TransitionContext(Auditor, DvRoles.InternalAuditor));
        Assert.Equal(DvWorkflowStatus.Submitted, dv.Status);
    }

    [Fact]
    public void Internal_auditor_can_return_the_dv_to_the_clerk()
    {
        var dv = FullyCertifiedDraft();
        dv.Fire(DvAction.RequestIaAudit, new TransitionContext(Clerk, DvRoles.Encoder));

        dv.Fire(DvAction.ReturnToClerk, new TransitionContext(Auditor, DvRoles.InternalAuditor));
        Assert.Equal(DvWorkflowStatus.Draft, dv.Status);
        Assert.Equal(DvLifecycleState.Draft, dv.Lifecycle);
    }

    [Fact]
    public void Transition_without_the_required_role_is_blocked_this_is_the_closed_set_workflow_status_gap()
    {
        var dv = FullyCertifiedDraft();
        RouteToAccounting(dv);

        // A user holding only the Encoder role tries to drive the Approve edge.
        var ex = Assert.Throws<UnauthorizedTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("mallory@cvsu", DvRoles.Encoder)));

        Assert.Equal("workflow.unauthorized", ex.Code);
        Assert.Equal(DvWorkflowStatus.Submitted, dv.Status); // state unchanged
    }

    [Fact]
    public void Encoder_cannot_approve_their_own_dv_SoD()
    {
        var dv = FullyCertifiedDraft(encoder: "clerk@cvsu");
        RouteToAccounting(dv);

        // The encoder also happens to hold the Accountant role — SoD still blocks.
        Assert.Throws<SegregationOfDutiesException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("clerk@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public void Head_of_agency_must_differ_from_the_approving_accountant_SoD()
    {
        var dv = FullyCertifiedDraft();
        RouteToAccounting(dv);
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));

        // Same person who approved now tries to approve-for-payment.
        Assert.Throws<SegregationOfDutiesException>(
            () => dv.Fire(DvAction.ApproveForPayment, new TransitionContext("accountant@cvsu", DvRoles.HeadOfAgency)));
    }

    [Fact]
    public void Approval_blocks_when_certifications_are_missing()
    {
        // Everything certified except the Budget Office fund-sufficiency.
        var dv = DraftWithBudgetLine();
        Certify(dv, Certification.InternalAudit, "auditor@cvsu");
        Certify(dv, Certification.EndUserAcceptance, "enduser@cvsu");
        Certify(dv, Certification.AccountantSignature, "boxd@cvsu");
        Certify(dv, Certification.SupplyPropertyInspection, "supply@cvsu");
        RouteToAccounting(dv);

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));

        Assert.Contains("Budget Office", ex.Message);
        Assert.Contains("R-DV-05", ex.Message);
    }

    [Fact]
    public void Net_amount_payable_is_gross_minus_tax_withheld()
    {
        var dv = new DisbursementVoucher("DV-2026-0009", Clerk, new Money(1000m), TestData.RegularAgencyFund())
        {
            TaxWithheld = new Money(150m),
        };
        Assert.Equal(new Money(850m), dv.NetAmountPayable);
    }

    [Fact]
    public void Approve_blocks_when_tax_withheld_exceeds_gross()
    {
        var dv = FullyCertifiedDraft();           // gross = 1000
        dv.TaxWithheld = new Money(1500m);
        RouteToAccounting(dv);

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));
        Assert.Contains("exceed", ex.Message);
    }

    [Fact]
    public void Approve_blocks_on_negative_tax_withheld()
    {
        var dv = FullyCertifiedDraft();
        dv.TaxWithheld = new Money(-5m);
        RouteToAccounting(dv);

        Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public void Administrator_role_is_exempt_from_SoD_the_documented_escape_hatch()
    {
        // The escape hatch is keyed on a HELD admin ROLE, not on the username.
        var admin = new TransitionContext("admin@cvsu", "System Manager");
        var dv = FullyCertifiedDraft(encoder: "admin@cvsu");
        dv.Fire(DvAction.RequestIaAudit, admin);
        dv.Fire(DvAction.Submit, admin);

        // Same elevated user approves their own DV — allowed by the escape hatch.
        dv.Fire(DvAction.Approve, admin);
        Assert.Equal(DvWorkflowStatus.Approved, dv.Status);
    }

    [Fact]
    public void Claiming_the_Administrator_username_without_an_admin_role_does_not_bypass_SoD()
    {
        // Regression: the dev header scheme lets a caller assert any X-User. Elevation
        // must come from a role claim, never from spelling the username "Administrator".
        var impostor = new TransitionContext("Administrator");   // username only, no role
        var dv = FullyCertifiedDraft(encoder: "Administrator");
        dv.Fire(DvAction.RequestIaAudit, new TransitionContext("Administrator", DvRoles.Encoder));
        dv.Fire(DvAction.Submit, new TransitionContext("auditor@cvsu", DvRoles.InternalAuditor));

        // Encoder == acting user, and no admin role held → SoD fires (and role gate too).
        Assert.Throws<UnauthorizedTransitionException>(() => dv.Fire(DvAction.Approve, impostor));
    }

    [Fact]
    public void Encoding_incomplete_dv_cannot_be_routed_for_audit()
    {
        // A draft missing its UACS budget line fails the encoding-complete gate (R-DV-02).
        var dv = new DisbursementVoucher("DV-2026-0002", Clerk, new Money(1000m), TestData.RegularAgencyFund());
        // No PapCode / LocationCode / ExpenseClass / ObjectAccountCode.

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.RequestIaAudit, new TransitionContext(Clerk, DvRoles.Encoder)));
        Assert.Contains("R-DV-02", ex.Message);
        Assert.Equal(DvWorkflowStatus.Draft, dv.Status); // unchanged
    }

    [Fact]
    public void Suppliers_dv_cannot_be_approved_without_supply_property_signoff()
    {
        var dv = FullyCertifiedDraft(dvType: DvType.Suppliers, supplyProperty: false);
        RouteToAccounting(dv);

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));
        Assert.Contains("Supply/Property", ex.Message);
        Assert.Contains("R-DV-07", ex.Message);
        Assert.Equal(DvWorkflowStatus.Submitted, dv.Status); // unchanged
    }

    [Fact]
    public void Non_suppliers_dv_does_not_require_the_supply_property_signoff()
    {
        // A Payroll DV has no Supply/Property step, so it approves without that sign-off.
        var dv = FullyCertifiedDraft(dvType: DvType.Payroll, supplyProperty: false);
        RouteToAccounting(dv);

        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Approved, dv.Status);
    }

    [Fact]
    public void Draft_dv_can_be_re_encoded_but_not_after_it_leaves_draft()
    {
        var dv = new DisbursementVoucher("DV-2026-0003", Clerk, new Money(500m), TestData.RegularAgencyFund(), DvType.Others);

        dv.UpdateEncoding(
            new Money(2500m), new Money(100m), TestData.StfFund(), DvType.Payroll,
            "100000100001000", "0102301000000", ExpenseClass.Mooe, "50203010");

        Assert.Equal(new Money(2500m), dv.Amount);
        Assert.Equal(new Money(100m), dv.TaxWithheld);
        Assert.Equal("05", dv.FundCluster.Code); // funding source (and thus cluster) re-encoded
        Assert.Equal(DvType.Payroll, dv.DvType);
        Assert.Equal("50203010", dv.ObjectAccountCode);

        // Route it out of Draft — further edits are now rejected.
        dv.Fire(DvAction.RequestIaAudit, new TransitionContext(Clerk, DvRoles.Encoder));
        Assert.Throws<InvalidTransitionException>(() => dv.UpdateEncoding(
            new Money(1m), Money.Zero, TestData.RegularAgencyFund(), DvType.Others,
            null, null, null, null));
    }

    [Fact]
    public void Control_number_cannot_be_reassigned_once_stamped()
    {
        var dv = FullyCertifiedDraft();
        dv.AssignControlNumber("DV-CN-01-2026-00001");

        Assert.Throws<InvalidTransitionException>(() => dv.AssignControlNumber("DV-CN-01-2026-00002"));
        Assert.Equal("DV-CN-01-2026-00001", dv.ControlNumber);
    }

    [Fact]
    public void Payment_can_only_be_recorded_once_the_dv_is_authorised_for_payment()
    {
        var dv = FullyCertifiedDraft();

        // Draft is far too early to attach a cheque/ADA reference.
        Assert.Throws<InvalidTransitionException>(() => dv.RecordPayment(DvPaymentMethod.Cheque, "CHK-001"));

        RouteToAccounting(dv);
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        dv.Fire(DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));

        dv.RecordPayment(DvPaymentMethod.Cheque, " CHK-001 ");
        Assert.Equal(DvPaymentMethod.Cheque, dv.PaymentMethod);
        Assert.Equal("CHK-001", dv.PaymentReference); // trimmed

        // A blank reference is rejected.
        Assert.Throws<InvalidTransitionException>(() => dv.RecordPayment(DvPaymentMethod.Ada, "  "));
    }

    [Fact]
    public void Certification_requires_the_responsible_role_and_the_encoder_cannot_self_certify()
    {
        var dv = DraftWithBudgetLine();

        // The encoder (Accounting Clerk) cannot assert the Budget Office certification.
        var ex = Assert.Throws<UnauthorizedTransitionException>(
            () => dv.Certify(Certification.BudgetSufficiency, new TransitionContext(Clerk, DvRoles.Encoder), CertTime));
        Assert.Equal("workflow.unauthorized", ex.Code);
        Assert.False(dv.BudgetCertified);

        // The Budget Officer can.
        dv.Certify(Certification.BudgetSufficiency, new TransitionContext("budget@cvsu", BudgetRoles.BudgetOfficer), CertTime);
        Assert.True(dv.BudgetCertified);
    }

    [Fact]
    public void Certification_records_the_certifier_and_timestamp()
    {
        var dv = DraftWithBudgetLine();
        dv.Certify(Certification.InternalAudit, new TransitionContext("ia@cvsu", DvRoles.InternalAuditor), CertTime);

        var state = dv.CertificationOf(Certification.InternalAudit);
        Assert.True(state.Done);
        Assert.Equal("ia@cvsu", state.By);
        Assert.Equal(CertTime, state.At);
    }

    [Fact]
    public void Certifications_are_locked_once_the_dv_is_approved()
    {
        var dv = FullyCertifiedDraft();
        RouteToAccounting(dv);
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));

        // Past the docstatus 0→1 boundary the certifications can no longer be (re)asserted.
        Assert.Throws<InvalidTransitionException>(() => dv.Certify(
            Certification.BudgetSufficiency, new TransitionContext("budget@cvsu", BudgetRoles.BudgetOfficer), CertTime));
    }

    [Fact]
    public void Administrator_is_still_blocked_by_a_missing_certification()
    {
        // The Administrator escape hatch is SoD-only — it does NOT bypass the cert gate.
        var dv = DraftWithBudgetLine(encoder: "Administrator");
        Certify(dv, Certification.InternalAudit, "auditor@cvsu");
        Certify(dv, Certification.EndUserAcceptance, "enduser@cvsu");
        Certify(dv, Certification.AccountantSignature, "boxd@cvsu");
        Certify(dv, Certification.SupplyPropertyInspection, "supply@cvsu");
        // Budget Office certification deliberately missing.
        dv.Fire(DvAction.RequestIaAudit, Admin());
        dv.Fire(DvAction.Submit, Admin());

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, Admin()));
        Assert.Contains("R-DV-05", ex.Message);
    }

    [Fact]
    public void Administrator_is_still_blocked_by_a_missing_supplier_signoff()
    {
        var dv = FullyCertifiedDraft(encoder: "Administrator", dvType: DvType.Suppliers, supplyProperty: false);
        dv.Fire(DvAction.RequestIaAudit, Admin());
        dv.Fire(DvAction.Submit, Admin());

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, Admin()));
        Assert.Contains("R-DV-07", ex.Message);
    }

    [Fact]
    public void Recorded_payment_instrument_cannot_be_silently_changed()
    {
        var dv = FullyCertifiedDraft();
        RouteToAccounting(dv);
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        dv.Fire(DvAction.ApproveForPayment, new TransitionContext("head@cvsu", DvRoles.HeadOfAgency));

        dv.RecordPayment(DvPaymentMethod.Cheque, "CHK-100");
        dv.RecordPayment(DvPaymentMethod.Cheque, "CHK-100"); // identical re-record is a no-op

        // Changing the instrument (different reference or method) is rejected.
        Assert.Throws<InvalidTransitionException>(() => dv.RecordPayment(DvPaymentMethod.Cheque, "CHK-999"));
        Assert.Throws<InvalidTransitionException>(() => dv.RecordPayment(DvPaymentMethod.Ada, "CHK-100"));
        Assert.Equal(DvPaymentMethod.Cheque, dv.PaymentMethod);
        Assert.Equal("CHK-100", dv.PaymentReference);
    }

    [Fact]
    public void Every_transition_is_role_gated()
    {
        // The Phase 2 invariant: no edge is gate-free (the legacy set_workflow_status hole).
        Assert.All(DvStateMachine.Transitions,
            t => Assert.False(string.IsNullOrWhiteSpace(t.RequiredRole),
                $"Transition {t.From} --{t.Action}--> {t.To} has no required role."));
    }

    [Fact]
    public void Transition_set_equals_the_legacy_frappe_workflow_drift_detection()
    {
        // The Phase 2 exit criterion: the single engine reproduces EXACTLY the legacy
        // Frappe DV workflow's transitions — read from the frozen workflow.json contract,
        // not a hand-copied list. Add/remove an edge on either side and this fails.
        var legacy = LoadLegacyTransitionTuples();

        var actual = DvStateMachine.Transitions
            .Select(t => (t.From, t.To, t.Action))
            .ToHashSet();

        Assert.Equal(legacy, actual);
    }

    // ── legacy-contract loading ────────────────────────────────────────────────

    // Maps the legacy Frappe workflow state / action names onto the rebuilt enums.
    private static readonly Dictionary<string, DvWorkflowStatus> StateMap = new()
    {
        ["Draft"] = DvWorkflowStatus.Draft,
        ["IA Audit Required"] = DvWorkflowStatus.IaAuditRequired,
        ["Submitted"] = DvWorkflowStatus.Submitted,
        ["Approved"] = DvWorkflowStatus.Approved,
        ["Approved for Payment"] = DvWorkflowStatus.ApprovedForPayment,
        ["Posted"] = DvWorkflowStatus.Posted,
        ["Released"] = DvWorkflowStatus.Released,
        ["Closed"] = DvWorkflowStatus.Closed,
    };

    private static readonly Dictionary<string, DvAction> ActionMap = new()
    {
        ["Route to IA"] = DvAction.RequestIaAudit,
        ["Submit to Accounting"] = DvAction.Submit,
        ["Return to Clerk"] = DvAction.ReturnToClerk,
        ["Approve"] = DvAction.Approve,
        ["Approve for Payment"] = DvAction.ApproveForPayment,
        ["Post"] = DvAction.Post,
        ["Release"] = DvAction.Release,
        ["Close"] = DvAction.Close,
    };

    private static HashSet<(DvWorkflowStatus, DvWorkflowStatus, DvAction)> LoadLegacyTransitionTuples()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "dv_workflow.json");
        Assert.True(File.Exists(path), $"Legacy workflow contract not found at {path}.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var tuples = new HashSet<(DvWorkflowStatus, DvWorkflowStatus, DvAction)>();

        foreach (var t in doc.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var from = t.GetProperty("state").GetString()!;
            var to = t.GetProperty("next_state").GetString()!;
            var action = t.GetProperty("action").GetString()!;

            Assert.True(StateMap.ContainsKey(from), $"Unmapped legacy state '{from}'.");
            Assert.True(StateMap.ContainsKey(to), $"Unmapped legacy state '{to}'.");
            Assert.True(ActionMap.ContainsKey(action), $"Unmapped legacy action '{action}'.");

            // Multiple legacy rows differ only by allowed-role (e.g. two "Route to IA"
            // rows); they collapse to one (from, to, action) edge here.
            tuples.Add((StateMap[from], StateMap[to], ActionMap[action]));
        }

        return tuples;
    }
}
