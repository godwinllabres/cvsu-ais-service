namespace CvSU.Ais.Domain.Common;

/// <summary>
/// Base for every business rule this domain enforces. Carries a stable
/// machine-readable <see cref="Code"/> so the API layer can map to
/// problem-details responses without string-matching the message.
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message) => Code = code;
}

/// <summary>A posted ledger row may never be mutated or deleted (R-GL-02).</summary>
public sealed class LedgerImmutabilityException(string message)
    : DomainException("ledger.immutable", message);

/// <summary>A journal batch whose debits and credits do not net to zero (R-GL-01).</summary>
public sealed class UnbalancedBatchException(string message)
    : DomainException("ledger.unbalanced", message);

/// <summary>A ledger line that is not strictly debit-XOR-credit.</summary>
public sealed class SingleSidedViolationException(string message)
    : DomainException("ledger.single_sided", message);

/// <summary>A UACS tuple missing one of its mandatory dimensions (R-BUD-06).</summary>
public sealed class UacsIncompleteException(string message)
    : DomainException("uacs.incomplete", message);

/// <summary>An obligation/allotment/disbursement that breaches its parent ceiling
/// (R-BUD-01 / R-BUD-02 / R-BUD-03).</summary>
public sealed class BudgetCeilingExceededException(string message)
    : DomainException("budget.ceiling_exceeded", message);

/// <summary>Internally Generated Funds (cluster 05/STF) cannot fund Personnel
/// Services (R-BUD-05).</summary>
public sealed class StfCannotFundPersonnelServicesException(string message)
    : DomainException("budget.stf_ps", message);

/// <summary>A single transaction that mixes two fund clusters — the thing that
/// would contaminate a per-cluster trial balance.</summary>
public sealed class FundClusterContaminationException(string message)
    : DomainException("budget.fund_cluster_contamination", message);

/// <summary>Segregation-of-duties breach: the same person filling two roles that
/// the controls require to be distinct (R-GL-04 / R-DV-09).</summary>
public sealed class SegregationOfDutiesException(string message)
    : DomainException("sod.violation", message);

/// <summary>An action that is not a legal edge out of the current workflow state.</summary>
public sealed class InvalidTransitionException(string message)
    : DomainException("workflow.invalid_transition", message);

/// <summary>The caller lacks the role this transition requires. This is the gap
/// that the legacy <c>set_workflow_status</c> left open by saving with
/// <c>ignore_permissions</c>; here every transition is role-gated.</summary>
public sealed class UnauthorizedTransitionException(string message)
    : DomainException("workflow.unauthorized", message);
