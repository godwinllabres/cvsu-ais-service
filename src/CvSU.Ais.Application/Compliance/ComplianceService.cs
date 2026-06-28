using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Compliance;

// ── COA Case ──────────────────────────────────────────────────────────────────

/// <summary>Thin list view of a COA audit case.</summary>
public sealed record CoaCaseView(
    string Name,
    string NdNcReference,
    string LiableParty,
    decimal Amount,
    string Status);

/// <summary>Full detail view of a COA audit case — all fields.</summary>
public sealed record CoaCaseDetailView(
    string Name,
    string NdNcReference,
    string? NfdReference,
    string? CoeReference,
    string LiableParty,
    decimal Amount,
    string? SettlementMode,
    string? OrReference,
    string Status,
    string? Remarks);

/// <summary>Inputs to open a new COA case (ND/NC received).</summary>
public sealed record CreateCoaCaseCommand(
    string NdNcReference,
    string LiableParty,
    decimal Amount,
    string? SettlementMode,
    string? Remarks);

/// <summary>
/// Orchestrates COA audit-case lifecycle transitions. Business rules are
/// limited to enforcing valid status progressions; no domain aggregate is used
/// because COA cases are entirely state-driven with no financial postings.
/// </summary>
public sealed class CoaCaseService(ICoaCaseRepository repo)
{
    private static readonly string[] ValidSettlementModes = ["DirectPayment", "PayrollDeduction"];

    public async Task<CoaCaseDetailView> CreateAsync(
        CreateCoaCaseCommand command,
        string generatedName,
        CancellationToken cancellationToken = default)
    {
        if (command.SettlementMode is not null
            && !Array.Exists(ValidSettlementModes, m => m == command.SettlementMode))
            throw new ArgumentException(
                $"Invalid SettlementMode '{command.SettlementMode}'. Allowed: {string.Join(", ", ValidSettlementModes)}.");

        var detail = new CoaCaseDetailView(
            generatedName,
            command.NdNcReference,
            NfdReference: null,
            CoeReference: null,
            command.LiableParty,
            command.Amount,
            command.SettlementMode,
            OrReference: null,
            Status: "NdNcReceived",
            command.Remarks);

        await repo.AddAsync(detail, cancellationToken);
        return detail;
    }

    public Task<IReadOnlyList<CoaCaseView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<CoaCaseDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"COA Case '{name}' not found.");

    public async Task RecordAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(name, cancellationToken);
        await repo.UpdateStatusAsync(name, "Recorded", cancellationToken);
    }

    public async Task IssueNfdAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(name, cancellationToken);
        await repo.UpdateStatusAsync(name, "NfdReceived", cancellationToken);
    }

    public async Task IssueCoeAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(name, cancellationToken);
        await repo.UpdateStatusAsync(name, "CoeIssued", cancellationToken);
    }

    public async Task SettleAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(name, cancellationToken);
        await repo.UpdateStatusAsync(name, "Settled", cancellationToken);
    }

    public async Task SubmitAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureExistsAsync(name, cancellationToken);
        await repo.UpdateStatusAsync(name, "SubmittedToCoa", cancellationToken);
    }

    private async Task EnsureExistsAsync(string name, CancellationToken cancellationToken)
    {
        _ = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"COA Case '{name}' not found.");
    }
}

// ── BIR 2307 ─────────────────────────────────────────────────────────────────

/// <summary>Thin list view of a BIR 2307 certificate.</summary>
public sealed record Bir2307View(
    string Name,
    string DvReference,
    string PayeeName,
    decimal GrossAmount,
    decimal EwtAmount,
    string ApprovalStatus);

/// <summary>Full detail view of a BIR 2307 certificate — all fields.</summary>
public sealed record Bir2307DetailView(
    string Name,
    string DvReference,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    string PayeeName,
    string PayeeTin,
    string? PayeeAddress,
    string IncomePaymentType,
    decimal GrossAmount,
    decimal EwtRate,
    decimal EwtAmount,
    decimal NetAmount,
    string ApprovalStatus,
    string? ReviewedBy,
    DateTime? ReviewedOn,
    string? Remarks);

/// <summary>Inputs to create a new BIR 2307 certificate in Draft status.</summary>
public sealed record CreateBir2307Command(
    string DvReference,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    string PayeeName,
    string PayeeTin,
    string? PayeeAddress,
    string IncomePaymentType,
    decimal GrossAmount,
    decimal EwtRate,
    string? Remarks);

/// <summary>
/// Orchestrates BIR 2307 certificate creation and approval workflow.
/// EWT amount and net amount are computed on creation and stored for
/// audit traceability.
/// </summary>
public sealed class Bir2307Service(IBir2307Repository repo)
{
    public async Task<Bir2307DetailView> CreateAsync(
        CreateBir2307Command command,
        string generatedName,
        CancellationToken cancellationToken = default)
    {
        var ewtAmount = Math.Round(command.GrossAmount * command.EwtRate / 100m, 2);
        var netAmount = command.GrossAmount - ewtAmount;

        var detail = new Bir2307DetailView(
            generatedName,
            command.DvReference,
            command.PeriodFrom,
            command.PeriodTo,
            command.PayeeName,
            command.PayeeTin,
            command.PayeeAddress,
            command.IncomePaymentType,
            command.GrossAmount,
            command.EwtRate,
            ewtAmount,
            netAmount,
            ApprovalStatus: "Draft",
            ReviewedBy: null,
            ReviewedOn: null,
            command.Remarks);

        await repo.AddAsync(detail, cancellationToken);
        return detail;
    }

    public Task<IReadOnlyList<Bir2307View>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<Bir2307DetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"BIR 2307 '{name}' not found.");

    public async Task<Bir2307DetailView> ApproveAsync(
        string name,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        _ = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"BIR 2307 '{name}' not found.");
        var reviewedOn = DateTime.UtcNow;
        await repo.UpdateStatusAsync(name, "Approved", reviewedBy, reviewedOn, cancellationToken);
        return (await repo.GetAsync(name, cancellationToken))!;
    }

    public async Task<Bir2307DetailView> RejectAsync(
        string name,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        _ = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"BIR 2307 '{name}' not found.");
        var reviewedOn = DateTime.UtcNow;
        await repo.UpdateStatusAsync(name, "Rejected", reviewedBy, reviewedOn, cancellationToken);
        return (await repo.GetAsync(name, cancellationToken))!;
    }
}

// ── Withholding Tax Statement ─────────────────────────────────────────────────

/// <summary>One tax-breakdown line within a Withholding Tax Statement.</summary>
public sealed record WhtLineDto(
    string TaxType,
    string? TaxClass,
    string? AtcCode,
    decimal Rate,
    decimal TaxBase,
    decimal TaxAmount,
    string? LiabilityAccount,
    string? SourceDv,
    string? Remarks);

/// <summary>Thin list view of a Withholding Tax Statement.</summary>
public sealed record WhtStatementView(
    string Name,
    string StatementType,
    DateOnly PostingDate,
    decimal GrossAmount,
    decimal TotalTaxAmount,
    string ApprovalStatus);

/// <summary>Full detail view of a Withholding Tax Statement including all lines.</summary>
public sealed record WhtStatementDetailView(
    string Name,
    string StatementType,
    DateOnly PostingDate,
    string TaxPeriodMonth,
    string? FundCluster,
    string FundingSourceCode,
    string? PayeeName,
    string? PayeeTin,
    decimal GrossAmount,
    decimal TotalTaxAmount,
    decimal NetAmount,
    string ApprovalStatus,
    string? ReviewedBy,
    DateTime? ReviewedOn,
    string? GlPostingReference,
    string? Remarks,
    IReadOnlyList<WhtLineDto> Lines);

/// <summary>Inputs to create a new Withholding Tax Statement in Draft status.</summary>
public sealed record CreateWhtStatementCommand(
    string StatementType,
    DateOnly PostingDate,
    string TaxPeriodMonth,
    string? FundCluster,
    string FundingSourceCode,
    string? PayeeName,
    string? PayeeTin,
    decimal GrossAmount,
    IReadOnlyList<WhtLineDto> Lines,
    string? Remarks);

/// <summary>
/// Orchestrates Withholding Tax Statement creation, approval, and GL posting.
/// TotalTaxAmount and NetAmount are derived from lines on creation.
/// <see cref="PostAsync"/> is only applicable to Remittance-type statements and posts:
/// Dr. Due to BIR (2020101000) = TotalTaxAmount / Cr. Cash–MDS Regular (1010404000).
/// </summary>
public sealed class WhtStatementService(
    IWhtStatementRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    public async Task<WhtStatementDetailView> CreateAsync(
        CreateWhtStatementCommand command,
        string generatedName,
        CancellationToken cancellationToken = default)
    {
        if (command.StatementType is not ("Accrual" or "Remittance"))
            throw new ArgumentException(
                $"Invalid StatementType '{command.StatementType}'. Allowed: Accrual, Remittance.");

        var totalTax = command.Lines.Sum(l => l.TaxAmount);
        var netAmount = command.GrossAmount - totalTax;

        var detail = new WhtStatementDetailView(
            generatedName,
            command.StatementType,
            command.PostingDate,
            command.TaxPeriodMonth,
            command.FundCluster,
            command.FundingSourceCode,
            command.PayeeName,
            command.PayeeTin,
            command.GrossAmount,
            totalTax,
            netAmount,
            ApprovalStatus: "Draft",
            ReviewedBy: null,
            ReviewedOn: null,
            GlPostingReference: null,
            command.Remarks,
            command.Lines);

        await repo.AddAsync(detail, command.Lines, cancellationToken);
        return detail;
    }

    public Task<IReadOnlyList<WhtStatementView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<WhtStatementDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"WHT Statement '{name}' not found.");

    public async Task<WhtStatementDetailView> ApproveAsync(
        string name,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        _ = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"WHT Statement '{name}' not found.");
        var reviewedOn = DateTime.UtcNow;
        await repo.UpdateStatusAsync(name, "Approved", reviewedBy, reviewedOn, cancellationToken);
        return (await repo.GetAsync(name, cancellationToken))!;
    }

    public async Task<WhtStatementDetailView> RejectAsync(
        string name,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        _ = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"WHT Statement '{name}' not found.");
        var reviewedOn = DateTime.UtcNow;
        await repo.UpdateStatusAsync(name, "Rejected", reviewedBy, reviewedOn, cancellationToken);
        return (await repo.GetAsync(name, cancellationToken))!;
    }

    /// <summary>
    /// Posts an Approved Remittance-type WHT Statement to the GL:
    /// Dr. Due to BIR / Cr. Cash–MDS Regular = TotalTaxAmount.
    /// Accrual-type statements are not posted here — they are captured via the
    /// payroll or DV GL entries that generate the original withholding.
    /// </summary>
    public Task<WhtStatementDetailView> PostAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"WHT Statement '{name}' not found.");

            if (detail.StatementType != "Remittance")
                throw new InvalidOperationException(
                    $"Only Remittance-type WHT statements can be posted. '{name}' is type '{detail.StatementType}'.");

            if (detail.ApprovalStatus != "Approved")
                throw new InvalidOperationException(
                    $"WHT Statement '{name}' must be Approved before posting (current: {detail.ApprovalStatus}).");

            var amount = new Money(detail.TotalTaxAmount);
            var today = detail.PostingDate;
            var fiscalYear = today.Year;

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.DueToBir, amount, Money.Zero,
                    "WHT Remittance Statement", name,
                    $"WHT remittance {detail.TaxPeriodMonth}"))
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashMdsRegular, Money.Zero, amount,
                    "WHT Remittance Statement", name,
                    $"WHT remittance {detail.TaxPeriodMonth}"));
            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateStatusAsync(name, "Posted", null, null, token);
            return (await repo.GetAsync(name, token))!;
        }, cancellationToken);
}

// ── State History ─────────────────────────────────────────────────────────────

/// <summary>View of a single workflow transition event.</summary>
public sealed record StateHistoryView(
    int Id,
    string ReferenceDoctype,
    string ReferenceName,
    string FromState,
    string ToState,
    string Action,
    string ActingUser,
    DateTime Timestamp,
    string? Remarks = null);

/// <summary>Records and retrieves workflow state transitions for any document.</summary>
public sealed class StateHistoryService(IStateHistoryRepository repo)
{
    public Task<IReadOnlyList<StateHistoryView>> ListForDocumentAsync(
        string doctype,
        string name,
        CancellationToken cancellationToken = default) =>
        repo.ListForDocumentAsync(doctype, name, cancellationToken);

    public Task RecordAsync(
        string doctype,
        string name,
        string from,
        string to,
        string action,
        string user,
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new StateHistoryView(
            Id: 0,
            doctype,
            name,
            from,
            to,
            action,
            user,
            DateTime.UtcNow,
            remarks);

        return repo.RecordAsync(entry, cancellationToken);
    }
}
