using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Domain.Disbursement;

/// <summary>
/// The Disbursement Voucher aggregate. State only ever changes through
/// <see cref="Fire"/>, which runs the three gates in order: legal edge → role →
/// guard. Nothing else mutates <see cref="Status"/>, so there is no equivalent of
/// the legacy <c>set_workflow_status(ignore_permissions=True)</c> back door.
/// </summary>
public sealed class DisbursementVoucher
{
    public string Name { get; }

    /// <summary>The user who created the DV (the SoD "encoder").</summary>
    public string Encoder { get; }

    /// <summary>Header amount payable.</summary>
    public Money Amount { get; }

    /// <summary>The fund key — cluster is read via <c>FundingSource.Cluster</c>, never stored apart.</summary>
    public FundingSource FundingSource { get; }

    public DvLifecycleState Lifecycle { get; private set; } = DvLifecycleState.Draft;
    public DvWorkflowStatus Status { get; private set; } = DvWorkflowStatus.Draft;

    public string? ApprovedBy { get; private set; }
    public string? ApprovedForPaymentBy { get; private set; }

    // Certifications and signature captured during the encode phase. The Post and
    // Approve guards read these; see Guards in DvWorkflow.cs.
    public bool BudgetCertified { get; set; }
    public bool InternalAuditConfirmed { get; set; }
    public bool EndUserConfirmed { get; set; }
    public bool AccountantSigned { get; set; }

    public DisbursementVoucher(string name, string encoder, Money amount, FundingSource fundingSource)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("DV name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(encoder))
            throw new ArgumentException("DV encoder is required.", nameof(encoder));

        Name = name;
        Encoder = encoder;
        Amount = amount;
        FundingSource = fundingSource ?? throw new ArgumentNullException(nameof(fundingSource));
    }

    /// <summary>The fund cluster, derived — never an independently-settable field.</summary>
    public FundCluster FundCluster => FundingSource.Cluster;

    /// <summary>
    /// Reconstitute an aggregate from persisted state. This is the persistence
    /// boundary — it restores a state the workflow already validated when the DV
    /// first reached it, so it deliberately does NOT re-run the transition guards.
    /// Only the repository should call this.
    /// </summary>
    public static DisbursementVoucher Rehydrate(
        string name,
        string encoder,
        Money amount,
        FundingSource fundingSource,
        DvLifecycleState lifecycle,
        DvWorkflowStatus status,
        string? approvedBy,
        string? approvedForPaymentBy,
        bool budgetCertified,
        bool internalAuditConfirmed,
        bool endUserConfirmed,
        bool accountantSigned) =>
        new(name, encoder, amount, fundingSource)
        {
            Lifecycle = lifecycle,
            Status = status,
            ApprovedBy = approvedBy,
            ApprovedForPaymentBy = approvedForPaymentBy,
            BudgetCertified = budgetCertified,
            InternalAuditConfirmed = internalAuditConfirmed,
            EndUserConfirmed = endUserConfirmed,
            AccountantSigned = accountantSigned,
        };

    /// <summary>
    /// Attempt a workflow action. Order of enforcement:
    /// 1) the action must be a legal edge out of the current state,
    /// 2) the caller must hold the transition's required role (no bypass), and
    /// 3) the transition's guard (SoD, certifications) must pass.
    /// Only then does the state change and post-transition bookkeeping run.
    /// </summary>
    public void Fire(DvAction action, TransitionContext context)
    {
        var transition = DvStateMachine.Resolve(Status, action);

        if (!context.HasRole(transition.RequiredRole))
            throw new UnauthorizedTransitionException(
                $"Action '{action}' on DV {Name} requires role '{transition.RequiredRole}'; " +
                $"caller '{context.ActingUser}' does not hold it.");

        transition.Guard?.Invoke(this, context);

        Status = transition.To;
        ApplyLifecycle(action);
        transition.OnApplied?.Invoke(this, context);
    }

    private void ApplyLifecycle(DvAction action)
    {
        Lifecycle = action switch
        {
            DvAction.Submit => DvLifecycleState.Submitted,
            DvAction.Reject => DvLifecycleState.Cancelled,
            _ => Lifecycle,
        };
    }

    internal void RecordApprovedBy(string user) => ApprovedBy = user;
    internal void RecordApprovedForPaymentBy(string user) => ApprovedForPaymentBy = user;
}
