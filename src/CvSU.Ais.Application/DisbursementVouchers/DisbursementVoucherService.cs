using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.DisbursementVouchers;

/// <summary>Inputs to create a draft DV. The encoder is the authenticated caller,
/// supplied by the API, not the client body.</summary>
public sealed record CreateDvCommand(
    string Encoder,
    int FiscalYear,
    decimal Amount,
    string FundingSourceCode,
    string? PapCode = null,
    string? LocationCode = null,
    ExpenseClass? ExpenseClass = null,
    string? ObjectAccountCode = null,
    bool BudgetCertified = false,
    bool InternalAuditConfirmed = false,
    bool EndUserConfirmed = false,
    bool AccountantSigned = false);

/// <summary>A read model of a DV's lifecycle state, returned after every command
/// and in list views — deliberately thin (a table row).</summary>
public sealed record DvStateView(
    string Name,
    string Lifecycle,
    string Status,
    string FundCluster,
    string? ApprovedBy,
    string? ApprovedForPaymentBy)
{
    public static DvStateView From(DisbursementVoucher dv) => new(
        dv.Name, dv.Lifecycle.ToString(), dv.Status.ToString(), dv.FundCluster.Code,
        dv.ApprovedBy, dv.ApprovedForPaymentBy);
}

/// <summary>
/// The full read model for a single DV — everything a clerk or approver needs to
/// process it: the amount, the fund + UACS budget line, the certifications, and
/// the lifecycle/approval state. The detail endpoint returns this so the UI can
/// show <em>what</em> is being paid, not just its status.
/// </summary>
public sealed record DvDetailView(
    string Name,
    int FiscalYear,
    string Encoder,
    decimal Amount,
    string Lifecycle,
    string Status,
    string FundingSourceCode,
    string FundClusterCode,
    string FundClusterName,
    string? PapCode,
    string? LocationCode,
    string? ExpenseClass,
    string? ObjectAccountCode,
    bool BudgetCertified,
    bool InternalAuditConfirmed,
    bool EndUserConfirmed,
    bool AccountantSigned,
    string? ApprovedBy,
    string? ApprovedForPaymentBy)
{
    public static DvDetailView From(DisbursementVoucher dv) => new(
        dv.Name,
        FiscalYearFromName(dv.Name),
        dv.Encoder,
        dv.Amount.Amount,
        dv.Lifecycle.ToString(),
        dv.Status.ToString(),
        dv.FundingSource.Code,
        dv.FundCluster.Code,
        dv.FundCluster.Name,
        dv.PapCode,
        dv.LocationCode,
        dv.ExpenseClass?.ToString(),
        dv.ObjectAccountCode,
        dv.BudgetCertified,
        dv.InternalAuditConfirmed,
        dv.EndUserConfirmed,
        dv.AccountantSigned,
        dv.ApprovedBy,
        dv.ApprovedForPaymentBy);

    /// <summary>The fiscal year lives in the gapless DV number (<c>DV-YYYY-#####</c>);
    /// parse it back for display rather than carrying a duplicate column.</summary>
    private static int FiscalYearFromName(string name)
    {
        var parts = name.Split('-');
        return parts.Length >= 2 && int.TryParse(parts[1], out var year)
            ? year
            : 0;
    }
}

/// <summary>
/// Orchestrates the DV lifecycle: assigns a gapless number on create and drives
/// role-gated transitions. All business rules live in the aggregate
/// (<see cref="DisbursementVoucher.Fire"/>); this service only coordinates the
/// transaction, numbering and persistence.
/// </summary>
public sealed class DisbursementVoucherService(
    IDisbursementVoucherRepository vouchers,
    IFundingSourceCatalog fundingSources,
    IVoucherNumberGenerator numbers,
    IGeneralLedger generalLedger,
    IBudgetLedger budgetLedger,
    IUnitOfWork unitOfWork)
{
    public Task<DvStateView> CreateAsync(CreateDvCommand command, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var fundingSource = await fundingSources.FindAsync(command.FundingSourceCode, token)
                ?? throw new KeyNotFoundException($"Unknown funding source '{command.FundingSourceCode}'.");

            var name = await numbers.NextAsync($"DV-{command.FiscalYear}", token);

            var voucher = new DisbursementVoucher(name, command.Encoder, new Money(command.Amount), fundingSource)
            {
                BudgetCertified = command.BudgetCertified,
                InternalAuditConfirmed = command.InternalAuditConfirmed,
                EndUserConfirmed = command.EndUserConfirmed,
                AccountantSigned = command.AccountantSigned,
                PapCode = command.PapCode,
                LocationCode = command.LocationCode,
                ExpenseClass = command.ExpenseClass,
                ObjectAccountCode = command.ObjectAccountCode,
            };

            await vouchers.AddAsync(voucher, token);
            return DvStateView.From(voucher);
        }, cancellationToken);

    public Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default) =>
        vouchers.ListAsync(cancellationToken);

    public async Task<DvDetailView> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var voucher = await vouchers.FindAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"DV '{name}' not found.");
        return DvDetailView.From(voucher);
    }

    /// <summary>
    /// Fire a workflow action and, where the transition is a posting point, write
    /// the ledgers in the same transaction so the status change and its postings
    /// commit atomically. Post = accrual GL; Release = cash GL + budget Disbursement.
    /// </summary>
    public Task<DvStateView> FireAsync(
        string name, DvAction action, TransitionContext context, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var voucher = await vouchers.FindAsync(name, token)
                ?? throw new KeyNotFoundException($"DV '{name}' not found.");

            voucher.Fire(action, context);

            switch (action)
            {
                case DvAction.Post:
                    await generalLedger.AppendBatchAsync(voucher.BuildAccrualPosting(Today), token);
                    break;
                case DvAction.Release:
                    var disbursement = voucher.BuildCashDisbursement(Today);
                    await generalLedger.AppendBatchAsync(disbursement.GeneralLedger, token);
                    await budgetLedger.AppendAsync(disbursement.BudgetEntry, token);
                    break;
            }

            await vouchers.UpdateAsync(voucher, token);
            return DvStateView.From(voucher);
        }, cancellationToken);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
