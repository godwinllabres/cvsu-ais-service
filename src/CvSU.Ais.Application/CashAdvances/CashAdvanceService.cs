using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.CashAdvances;

// ─── DTOs ───────────────────────────────────────────────────────────────────

public sealed record LiquidationLineDto(
    string ExpenseType,
    string? Description,
    decimal Amount,
    string? ReceiptReference,
    DateOnly? ReceiptDate,
    string? AccountCode);

public sealed record CashAdvanceView(
    string Name,
    string Employee,
    string EmployeeName,
    decimal AdvanceAmount,
    decimal LiquidatedAmount,
    string Status);

public sealed record CashAdvanceDetailView(
    string Name,
    string Employee,
    string EmployeeName,
    DateOnly PostingDate,
    string? FundCluster,
    string Purpose,
    decimal AdvanceAmount,
    decimal LiquidatedAmount,
    decimal UnliquidatedBalance,
    DateOnly DueDate,
    string Status,
    string? GlPostingReference,
    string? Remarks);

public sealed record CreateCashAdvanceCommand(
    string Employee,
    string EmployeeName,
    DateOnly PostingDate,
    string? FundCluster,
    string Purpose,
    decimal AdvanceAmount,
    DateOnly DueDate,
    string? Remarks);

public sealed record LiquidationReportView(
    string Name,
    string CashAdvanceName,
    string EmployeeName,
    decimal TotalLiquidated,
    string Status);

public sealed record LiquidationReportDetailView(
    string Name,
    string CashAdvanceName,
    string Employee,
    string EmployeeName,
    DateOnly PostingDate,
    string? FundCluster,
    decimal TotalLiquidated,
    decimal AdvanceAmount,
    decimal RefundDue,
    decimal ReimbursementDue,
    string Status,
    string? GlPostingReference,
    string? Remarks,
    IReadOnlyList<LiquidationLineDto> Lines);

public sealed record CreateLiquidationReportCommand(
    string CashAdvanceName,
    DateOnly PostingDate,
    string? FundCluster,
    IReadOnlyList<LiquidationLineDto> Lines,
    string? Remarks);

// ─── Services ───────────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates cash advance creation and disbursement. <see cref="AdvanceAsync"/>
/// posts the GL entry (Dr. Advances-SDO / Cr. Cash-MDS) when the advance is released.
/// </summary>
public sealed class CashAdvanceService(
    ICashAdvanceRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "Cash Advance";

    public async Task<string> CreateAsync(
        CreateCashAdvanceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AdvanceAmount <= 0m)
            throw new ArgumentException("Cash advance amount must be greater than zero.", nameof(command));

        return await repo.AddAsync(command, cancellationToken);
    }

    public Task<IReadOnlyList<CashAdvanceView>> ListAsync(
        CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<CashAdvanceDetailView> GetAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Cash advance '{name}' not found.");

    /// <summary>
    /// Transitions the cash advance from Draft to Advanced and posts the disbursement
    /// GL entry: Dr. Advances to SDO (1990101000) / Cr. Cash–MDS Regular (1010404000).
    /// </summary>
    public Task AdvanceAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"Cash advance '{name}' not found.");

            if (detail.Status != "Draft")
                throw new InvalidOperationException(
                    $"Cannot advance '{name}' from status '{detail.Status}'; expected Draft.");

            var amount = new Money(detail.AdvanceAmount);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fiscalYear = today.Year;

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.AdvancesToSdo, amount, Money.Zero,
                    DocType, name, "Cash advance granted to SDO"))
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashMdsRegular, Money.Zero, amount,
                    DocType, name, "Cash advance granted to SDO"));
            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateStatusAsync(name, "Advanced", token);
            return 0;
        }, cancellationToken);

    /// <summary>Cancels a cash advance that has not yet been released.</summary>
    public async Task CancelAsync(string name, CancellationToken cancellationToken = default)
    {
        var detail = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Cash advance '{name}' not found.");

        if (detail.Status is "FullyLiquidated" or "Cancelled")
            throw new InvalidOperationException(
                $"Cannot cancel cash advance '{name}' with status '{detail.Status}'.");

        await repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
    }
}

/// <summary>
/// Orchestrates liquidation report lifecycle. <see cref="PostAsync"/> posts the full
/// settlement journal: debits each expense line, credits the SDO advance receivable,
/// and handles refund (excess cash returned) or reimbursement (additional cash owed)
/// as additional debit / credit lines respectively.
/// </summary>
public sealed class LiquidationReportService(
    ILiquidationReportRepository repo,
    ICashAdvanceRepository cashAdvanceRepo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "Liquidation Report";

    public async Task<string> CreateAsync(
        CreateLiquidationReportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.Lines.Any())
            throw new InvalidOperationException("Liquidation report must have at least one expense line.");
        if (command.Lines.Any(l => l.Amount <= 0))
            throw new InvalidOperationException("Liquidation line amounts must be positive.");
        return await repo.AddAsync(command, cancellationToken);
    }

    public Task<IReadOnlyList<LiquidationReportView>> ListAsync(
        CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<LiquidationReportDetailView> GetAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Liquidation report '{name}' not found.");

    public async Task SubmitAsync(string name, CancellationToken cancellationToken = default)
    {
        var detail = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Liquidation report '{name}' not found.");
        if (detail.Status != "Draft")
            throw new InvalidOperationException(
                $"Cannot submit '{name}' from status '{detail.Status}'; expected Draft.");
        await repo.UpdateStatusAsync(name, "Submitted", cancellationToken);
    }

    /// <summary>
    /// Posts the liquidation to the accrual GL and marks the parent cash advance's
    /// liquidated balance. Journal construction:
    /// <list type="bullet">
    ///   <item>Dr. {line.AccountCode} per expense line</item>
    ///   <item>Dr. Cash-MDS = refund_due (if employee returns excess cash)</item>
    ///   <item>Cr. Advances to SDO = advance_amount (clears the receivable)</item>
    ///   <item>Cr. Cash-MDS = reimbursement_due (if agency owes employee more cash)</item>
    /// </list>
    /// </summary>
    public Task PostAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"Liquidation report '{name}' not found.");

            if (detail.Status != "Submitted")
                throw new InvalidOperationException(
                    $"Cannot post '{name}' from status '{detail.Status}'; expected Submitted.");

            // Load the parent cash advance and guard against double-liquidation (F18):
            // it must currently be Advanced (not already FullyLiquidated/Settled), otherwise
            // a second liquidation post would credit Advances to SDO more than once.
            var parent = await cashAdvanceRepo.GetAsync(detail.CashAdvanceName, token)
                ?? throw new KeyNotFoundException(
                    $"Cash advance '{detail.CashAdvanceName}' not found.");

            if (parent.Status != "Advanced")
                throw new InvalidOperationException(
                    $"Cash advance '{detail.CashAdvanceName}' must be Advanced to liquidate " +
                    $"(current: {parent.Status}); it may already be liquidated or settled.");

            if (detail.TotalLiquidated > parent.AdvanceAmount)
                throw new InvalidOperationException(
                    $"Liquidation total ({detail.TotalLiquidated:N2}) exceeds the cash advance " +
                    $"amount ({parent.AdvanceAmount:N2}).");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fiscalYear = today.Year;
            var advance = new Money(detail.AdvanceAmount);
            var refund = new Money(detail.RefundDue);
            var reimbursement = new Money(detail.ReimbursementDue);

            var batch = new GlPostingBatch();

            // Debit each documented expense to its account.
            foreach (var line in detail.Lines)
            {
                var account = string.IsNullOrWhiteSpace(line.AccountCode)
                    ? GlAccounts.MiscExpense
                    : line.AccountCode;
                batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                    account, new Money(line.Amount), Money.Zero,
                    DocType, name, line.ExpenseType));
            }

            // If employee returns excess cash (refund_due > 0), debit Cash-MDS.
            if (refund.IsPositive)
                batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashMdsRegular, refund, Money.Zero,
                    DocType, name, "Refund of excess cash advance"));

            // Clear the SDO advance receivable for the full advance amount.
            batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                GlAccounts.AdvancesToSdo, Money.Zero, advance,
                DocType, name, "Settlement of cash advance"));

            // If agency owes employee additional cash (reimbursement_due > 0), credit Cash-MDS.
            if (reimbursement.IsPositive)
                batch.Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashMdsRegular, Money.Zero, reimbursement,
                    DocType, name, "Reimbursement of excess expense"));

            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateCashAdvanceLiquidatedAsync(detail.CashAdvanceName, detail.TotalLiquidated, token);

            // Settle the parent so a second liquidation post is rejected (F18). The full
            // advance receivable was just credited, so the advance is now liquidated even
            // when the documented expenses are less than the advance amount.
            await cashAdvanceRepo.UpdateStatusAsync(detail.CashAdvanceName, "FullyLiquidated", token);

            await repo.UpdateStatusAsync(name, "Posted", token);
            return 0;
        }, cancellationToken);

    public async Task CancelAsync(string name, CancellationToken cancellationToken = default)
    {
        var detail = await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Liquidation report '{name}' not found.");
        if (detail.Status is "Posted" or "Cancelled")
            throw new InvalidOperationException(
                $"Cannot cancel '{name}' with status '{detail.Status}'.");
        await repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
    }
}
