using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
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

    private static DisbursementVoucher FullyCertifiedDraft(string encoder = Clerk)
    {
        var dv = new DisbursementVoucher("DV-2026-0001", encoder, new Money(1000m), TestData.RegularAgencyFund())
        {
            BudgetCertified = true,
            InternalAuditConfirmed = true,
            EndUserConfirmed = true,
            AccountantSigned = true,
        };
        return dv;
    }

    private static void Submit(DisbursementVoucher dv) =>
        dv.Fire(DvAction.Submit, new TransitionContext(dv.Encoder, DvRoles.Encoder));

    [Fact]
    public void Happy_path_runs_draft_to_closed_with_the_right_roles()
    {
        var dv = FullyCertifiedDraft();

        Submit(dv);
        Assert.Equal(DvWorkflowStatus.Submitted, dv.Status);
        Assert.Equal(DvLifecycleState.Submitted, dv.Lifecycle);

        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));
        Assert.Equal(DvWorkflowStatus.Approved, dv.Status);
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
        Submit(dv);

        // Post is only legal from ApprovedForPayment, never from Submitted.
        Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Post, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public void Transition_without_the_required_role_is_blocked_this_is_the_closed_set_workflow_status_gap()
    {
        var dv = FullyCertifiedDraft();
        Submit(dv);

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
        Submit(dv);

        // The encoder also happens to hold the Accountant role — SoD still blocks.
        Assert.Throws<SegregationOfDutiesException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("clerk@cvsu", DvRoles.Accountant)));
    }

    [Fact]
    public void Head_of_agency_must_differ_from_the_approving_accountant_SoD()
    {
        var dv = FullyCertifiedDraft();
        Submit(dv);
        dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant));

        // Same person who approved now tries to approve-for-payment.
        Assert.Throws<SegregationOfDutiesException>(
            () => dv.Fire(DvAction.ApproveForPayment, new TransitionContext("accountant@cvsu", DvRoles.HeadOfAgency)));
    }

    [Fact]
    public void Approval_blocks_when_certifications_are_missing()
    {
        var dv = FullyCertifiedDraft();
        dv.BudgetCertified = false;
        Submit(dv);

        var ex = Assert.Throws<InvalidTransitionException>(
            () => dv.Fire(DvAction.Approve, new TransitionContext("accountant@cvsu", DvRoles.Accountant)));

        Assert.Contains("Budget Office", ex.Message);
        Assert.Contains("R-DV-05", ex.Message);
    }

    [Fact]
    public void Administrator_is_exempt_from_SoD_the_documented_escape_hatch()
    {
        var dv = FullyCertifiedDraft(encoder: "Administrator");
        dv.Fire(DvAction.Submit, new TransitionContext("Administrator"));

        // Same Administrator approves their own DV — allowed by the escape hatch.
        dv.Fire(DvAction.Approve, new TransitionContext("Administrator"));
        Assert.Equal(DvWorkflowStatus.Approved, dv.Status);
    }

    [Fact]
    public void Transition_table_matches_the_intended_workflow_drift_detection()
    {
        // If anyone adds/removes an edge, this fails — forcing the workflow change to
        // be deliberate, the Phase 2 exit criterion.
        var expected = new HashSet<(DvWorkflowStatus, DvWorkflowStatus, DvAction)>
        {
            (DvWorkflowStatus.Draft, DvWorkflowStatus.IaAuditRequired, DvAction.RequestIaAudit),
            (DvWorkflowStatus.Draft, DvWorkflowStatus.Submitted, DvAction.Submit),
            (DvWorkflowStatus.IaAuditRequired, DvWorkflowStatus.Submitted, DvAction.Submit),
            (DvWorkflowStatus.Submitted, DvWorkflowStatus.Approved, DvAction.Approve),
            (DvWorkflowStatus.Submitted, DvWorkflowStatus.Rejected, DvAction.Reject),
            (DvWorkflowStatus.Approved, DvWorkflowStatus.ApprovedForPayment, DvAction.ApproveForPayment),
            (DvWorkflowStatus.Approved, DvWorkflowStatus.Rejected, DvAction.Reject),
            (DvWorkflowStatus.ApprovedForPayment, DvWorkflowStatus.Posted, DvAction.Post),
            (DvWorkflowStatus.Posted, DvWorkflowStatus.Released, DvAction.Release),
            (DvWorkflowStatus.Released, DvWorkflowStatus.Closed, DvAction.Close),
        };

        var actual = DvStateMachine.Transitions
            .Select(t => (t.From, t.To, t.Action))
            .ToHashSet();

        Assert.Equal(expected, actual);
    }
}
