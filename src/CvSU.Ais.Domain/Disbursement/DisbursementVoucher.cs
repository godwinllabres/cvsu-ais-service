using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;
using CvSU.Ais.Domain.Ledgers;

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

    /// <summary>Header amount payable. Mutable only while Draft (via <see cref="UpdateEncoding"/>).</summary>
    public Money Amount { get; private set; }

    /// <summary>The fund key — cluster is read via <c>FundingSource.Cluster</c>, never stored apart.
    /// Mutable only while Draft (via <see cref="UpdateEncoding"/>).</summary>
    public FundingSource FundingSource { get; private set; }

    /// <summary>What the DV pays for. Drives type-specific encoding gates (Suppliers
    /// DVs need a Supply/Property sign-off before approval). Mutable only while Draft.</summary>
    public DvType DvType { get; private set; }

    public DvLifecycleState Lifecycle { get; private set; } = DvLifecycleState.Draft;
    public DvWorkflowStatus Status { get; private set; } = DvWorkflowStatus.Draft;

    public string? ApprovedBy { get; private set; }
    public string? ApprovedForPaymentBy { get; private set; }

    /// <summary>The gapless control number (DV-CN-&lt;cluster&gt;-YYYY-#####) assigned
    /// at approval. Null until the DV is approved.</summary>
    public string? ControlNumber { get; private set; }

    /// <summary>How the DV was settled and the instrument's reference (cheque/ADA/
    /// transfer no). Captured by the cashier around release; the reference is unique
    /// per method across all DVs (enforced by the repository).</summary>
    public DvPaymentMethod? PaymentMethod { get; private set; }
    public string? PaymentReference { get; private set; }

    // Certifications — each is asserted by the officer RESPONSIBLE for it via
    // <see cref="Certify"/> (never by the encoder), and recorded with the certifier's
    // identity and timestamp (the audit trail). The Approve/Post guards read the bools;
    // every field is restored by Rehydrate and is otherwise settable only through Certify.
    public bool BudgetCertified { get; private set; }
    public string? BudgetCertifiedBy { get; private set; }
    public DateTime? BudgetCertifiedAt { get; private set; }

    public bool InternalAuditConfirmed { get; private set; }
    public string? InternalAuditConfirmedBy { get; private set; }
    public DateTime? InternalAuditConfirmedAt { get; private set; }

    public bool EndUserConfirmed { get; private set; }
    public string? EndUserConfirmedBy { get; private set; }
    public DateTime? EndUserConfirmedAt { get; private set; }

    public bool AccountantSigned { get; private set; }
    public string? AccountantSignedBy { get; private set; }
    public DateTime? AccountantSignedAt { get; private set; }

    /// <summary>Supply/Property Officer inspection-and-acceptance sign-off. The approval
    /// gate requires it for <see cref="DvType.Suppliers"/> DVs.</summary>
    public bool SupplyPropertySignedOff { get; private set; }
    public string? SupplyPropertySignedOffBy { get; private set; }
    public DateTime? SupplyPropertySignedOffAt { get; private set; }

    /// <summary>Expanded Withholding Tax (EWT) deducted from the gross <see cref="Amount"/>.
    /// Captured during the encode phase; the approval gate enforces 0 ≤ tax ≤ gross.</summary>
    public Money TaxWithheld { get; set; } = Money.Zero;

    /// <summary>Cash actually paid to the payee — gross <see cref="Amount"/> minus
    /// <see cref="TaxWithheld"/>. The gross is the obligation/accrual; the net is the
    /// cash disbursement (the difference is remitted to the BIR as withheld tax).</summary>
    public Money NetAmountPayable => Amount - TaxWithheld;

    // The UACS budget line this DV charges, captured during the encode phase. It
    // is required before Posting, where the accrual GL and budget Disbursement
    // entries are built from it.
    public string? PapCode { get; set; }
    public string? LocationCode { get; set; }
    public ExpenseClass? ExpenseClass { get; set; }
    public string? ObjectAccountCode { get; set; }

    private const string DocType = "AIS Disbursement Voucher";

    public DisbursementVoucher(
        string name, string encoder, Money amount, FundingSource fundingSource, DvType dvType = DvType.Suppliers)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("DV name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(encoder))
            throw new ArgumentException("DV encoder is required.", nameof(encoder));

        Name = name;
        Encoder = encoder;
        Amount = amount;
        FundingSource = fundingSource ?? throw new ArgumentNullException(nameof(fundingSource));
        DvType = dvType;
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
        DvType dvType,
        DvLifecycleState lifecycle,
        DvWorkflowStatus status,
        string? approvedBy,
        string? approvedForPaymentBy,
        CertificationState budget,
        CertificationState internalAudit,
        CertificationState endUser,
        CertificationState accountant,
        CertificationState supplyProperty,
        string? papCode,
        string? locationCode,
        ExpenseClass? expenseClass,
        string? objectAccountCode,
        string? controlNumber,
        DvPaymentMethod? paymentMethod,
        string? paymentReference,
        Money taxWithheld = default) =>
        new(name, encoder, amount, fundingSource, dvType)
        {
            Lifecycle = lifecycle,
            Status = status,
            ApprovedBy = approvedBy,
            ApprovedForPaymentBy = approvedForPaymentBy,
            BudgetCertified = budget.Done,
            BudgetCertifiedBy = budget.By,
            BudgetCertifiedAt = budget.At,
            InternalAuditConfirmed = internalAudit.Done,
            InternalAuditConfirmedBy = internalAudit.By,
            InternalAuditConfirmedAt = internalAudit.At,
            EndUserConfirmed = endUser.Done,
            EndUserConfirmedBy = endUser.By,
            EndUserConfirmedAt = endUser.At,
            AccountantSigned = accountant.Done,
            AccountantSignedBy = accountant.By,
            AccountantSignedAt = accountant.At,
            SupplyPropertySignedOff = supplyProperty.Done,
            SupplyPropertySignedOffBy = supplyProperty.By,
            SupplyPropertySignedOffAt = supplyProperty.At,
            PapCode = papCode,
            LocationCode = locationCode,
            ExpenseClass = expenseClass,
            ObjectAccountCode = objectAccountCode,
            ControlNumber = controlNumber,
            PaymentMethod = paymentMethod,
            PaymentReference = paymentReference,
            TaxWithheld = taxWithheld,
        };

    /// <summary>Build the UACS line this DV charges; throws
    /// <see cref="UacsIncompleteException"/> if the encoder left it incomplete.</summary>
    public UacsCode RequireBudgetLine()
    {
        if (ExpenseClass is null)
            throw new UacsIncompleteException(
                "DV budget line is incomplete: expense class is required before posting.");

        // The UacsCode constructor rejects any missing string dimension.
        return new UacsCode(FundingSource, PapCode!, LocationCode!, ExpenseClass.Value, ObjectAccountCode!);
    }

    /// <summary>Accrual posting fired on Post: DR expense object account / CR accounts payable.</summary>
    public GlPostingBatch BuildAccrualPosting(DateOnly postingDate)
    {
        var uacs = RequireBudgetLine();
        var fiscalYear = postingDate.Year;

        var batch = new GlPostingBatch()
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, uacs.ObjectAccountCode, Amount, Money.Zero, DocType, Name, "Accrual of payable"))
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, GlAccounts.AccountsPayable, Money.Zero, Amount, DocType, Name, "Accrual of payable"));

        batch.EnsureBalanced();
        return batch;
    }

    /// <summary>Cash disbursement fired on Release: DR accounts payable / CR Cash-MDS,
    /// plus the budget-registry Disbursement entry against this DV's UACS line.</summary>
    public CashDisbursement BuildCashDisbursement(DateOnly postingDate)
    {
        var uacs = RequireBudgetLine();
        var fiscalYear = postingDate.Year;

        var gl = new GlPostingBatch()
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, GlAccounts.AccountsPayable, Amount, Money.Zero, DocType, Name, "Cash disbursement"))
            .Add(new GeneralLedgerEntry(postingDate, fiscalYear, GlAccounts.CashMdsRegular, Money.Zero, Amount, DocType, Name, "Cash disbursement"));
        gl.EnsureBalanced();

        var budgetEntry = new BudgetLedgerEntry(
            postingDate, fiscalYear, uacs, BudgetEntryType.Disbursement, Amount, DocType, Name);

        return new CashDisbursement(gl, budgetEntry);
    }

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
        ApplyLifecycle();
        transition.OnApplied?.Invoke(this, context);
    }

    // Frappe's docstatus, made explicit. The workflow states up to and including
    // "Submitted" are doc_status 0 (still editable); "Approved" onward are doc_status 1
    // (locked). The legacy app flips docstatus at the Approve step — NOT at the workflow
    // "Submitted" state — so the lifecycle is derived from the target status, not the
    // action. (Cancelled, the doc_status-2 analogue, is reserved for a future cancel op.)
    private void ApplyLifecycle()
    {
        Lifecycle = Status switch
        {
            DvWorkflowStatus.Draft or DvWorkflowStatus.IaAuditRequired or DvWorkflowStatus.Submitted
                => DvLifecycleState.Draft,
            _ => DvLifecycleState.Submitted,
        };
    }

    internal void RecordApprovedBy(string user) => ApprovedBy = user;
    internal void RecordApprovedForPaymentBy(string user) => ApprovedForPaymentBy = user;

    /// <summary>
    /// Stamp the gapless control number on approval. Idempotency guard: a DV that
    /// already carries a control number cannot be re-numbered, so a re-approval can
    /// never burn a second number from the series. Called by the application service
    /// (numbering is async and lives in infrastructure), not from a transition effect.
    /// </summary>
    public void AssignControlNumber(string controlNumber)
    {
        if (string.IsNullOrWhiteSpace(controlNumber))
            throw new ArgumentException("Control number is required.", nameof(controlNumber));
        if (!string.IsNullOrWhiteSpace(ControlNumber))
            throw new InvalidTransitionException(
                $"DV {Name} already has control number {ControlNumber}; it cannot be reassigned.");
        ControlNumber = controlNumber;
    }

    /// <summary>
    /// Re-encode the DV's editable payload. Allowed only while the DV is in
    /// <see cref="DvWorkflowStatus.Draft"/> (the initial draft or one returned to the
    /// clerk) — once it leaves Draft the encoding is locked, so a corrected budget line
    /// can be supplied before the encoding-complete gate, but never after audit/approval.
    /// </summary>
    public void UpdateEncoding(
        Money amount,
        Money taxWithheld,
        FundingSource fundingSource,
        DvType dvType,
        string? papCode,
        string? locationCode,
        ExpenseClass? expenseClass,
        string? objectAccountCode)
    {
        if (Status != DvWorkflowStatus.Draft)
            throw new InvalidTransitionException(
                $"DV {Name} can only be edited while in Draft; it is '{Status}'. " +
                "Return it to the clerk before editing.");

        Amount = amount;
        TaxWithheld = taxWithheld;
        FundingSource = fundingSource ?? throw new ArgumentNullException(nameof(fundingSource));
        DvType = dvType;
        PapCode = papCode;
        LocationCode = locationCode;
        ExpenseClass = expenseClass;
        ObjectAccountCode = objectAccountCode;
        // Certifications are NOT touched here — they are asserted by their responsible
        // officers via Certify() and persist across a Draft re-encode.
    }

    /// <summary>
    /// Record a certification, asserted by the officer responsible for it. The caller must
    /// hold that certification's role (Administrator excepted), and the DV must still be in
    /// the pre-approval phase (Draft lifecycle). Captures the certifier's identity and the
    /// timestamp as the audit trail. Re-certifying simply refreshes who/when.
    /// </summary>
    public void Certify(Certification certification, TransitionContext context, DateTime whenUtc)
    {
        if (Lifecycle != DvLifecycleState.Draft)
            throw new InvalidTransitionException(
                $"DV {Name} can no longer be certified; it is '{Status}'. " +
                "Certifications are captured before approval.");

        var requiredRole = Certifications.RequiredRole(certification);
        if (!context.HasRole(requiredRole))
            throw new UnauthorizedTransitionException(
                $"Certification '{certification}' on DV {Name} requires role '{requiredRole}'; " +
                $"caller '{context.ActingUser}' does not hold it.");

        var who = context.ActingUser;
        switch (certification)
        {
            case Certification.BudgetSufficiency:
                BudgetCertified = true; BudgetCertifiedBy = who; BudgetCertifiedAt = whenUtc; break;
            case Certification.InternalAudit:
                InternalAuditConfirmed = true; InternalAuditConfirmedBy = who; InternalAuditConfirmedAt = whenUtc; break;
            case Certification.EndUserAcceptance:
                EndUserConfirmed = true; EndUserConfirmedBy = who; EndUserConfirmedAt = whenUtc; break;
            case Certification.AccountantSignature:
                AccountantSigned = true; AccountantSignedBy = who; AccountantSignedAt = whenUtc; break;
            case Certification.SupplyPropertyInspection:
                SupplyPropertySignedOff = true; SupplyPropertySignedOffBy = who; SupplyPropertySignedOffAt = whenUtc; break;
            default:
                throw new ArgumentOutOfRangeException(nameof(certification), certification, "Unknown certification.");
        }
    }

    /// <summary>The audit-trail view of one certification (for the read model).</summary>
    public CertificationState CertificationOf(Certification certification) => certification switch
    {
        Certification.BudgetSufficiency => new(BudgetCertified, BudgetCertifiedBy, BudgetCertifiedAt),
        Certification.InternalAudit => new(InternalAuditConfirmed, InternalAuditConfirmedBy, InternalAuditConfirmedAt),
        Certification.EndUserAcceptance => new(EndUserConfirmed, EndUserConfirmedBy, EndUserConfirmedAt),
        Certification.AccountantSignature => new(AccountantSigned, AccountantSignedBy, AccountantSignedAt),
        Certification.SupplyPropertyInspection => new(SupplyPropertySignedOff, SupplyPropertySignedOffBy, SupplyPropertySignedOffAt),
        _ => throw new ArgumentOutOfRangeException(nameof(certification), certification, "Unknown certification."),
    };

    /// <summary>
    /// Capture how the DV was paid (cheque/ADA/transfer reference). Allowed only once
    /// the DV is authorised for payment, so a reference can't be attached to a draft.
    /// The reference's cross-DV uniqueness is enforced by the repository <em>before</em>
    /// this is called — the aggregate cannot see other DVs.
    /// </summary>
    public void RecordPayment(DvPaymentMethod method, string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new InvalidTransitionException("A payment reference is required to record a payment.");
        if (Status is not (DvWorkflowStatus.ApprovedForPayment or DvWorkflowStatus.Posted or DvWorkflowStatus.Released))
            throw new InvalidTransitionException(
                $"Payment details can only be recorded once the DV is approved for payment; DV {Name} is '{Status}'.");

        var trimmed = reference.Trim();
        // One-shot: the recorded instrument is the durable identity of the disbursement.
        // Re-recording the identical instrument is a harmless no-op; changing it is forbidden
        // (correcting a wrong instrument must go through an explicit, audited operation).
        if (PaymentReference is not null && (PaymentMethod != method || PaymentReference != trimmed))
            throw new InvalidTransitionException(
                $"DV {Name} already records payment {PaymentMethod} '{PaymentReference}'; " +
                "the disbursement instrument cannot be silently changed.");

        PaymentMethod = method;
        PaymentReference = trimmed;
    }
}
