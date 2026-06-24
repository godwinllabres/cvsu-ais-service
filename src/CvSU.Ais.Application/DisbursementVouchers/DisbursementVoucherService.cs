using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;

namespace CvSU.Ais.Application.DisbursementVouchers;

/// <summary>Inputs to create a draft DV. The encoder is the authenticated caller,
/// supplied by the API, not the client body.</summary>
public sealed record CreateDvCommand(
    string Encoder,
    int FiscalYear,
    decimal Amount,
    string FundingSourceCode,
    bool BudgetCertified = false,
    bool InternalAuditConfirmed = false,
    bool EndUserConfirmed = false,
    bool AccountantSigned = false);

/// <summary>A read model of a DV's lifecycle state, returned after every command.</summary>
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
/// Orchestrates the DV lifecycle: assigns a gapless number on create and drives
/// role-gated transitions. All business rules live in the aggregate
/// (<see cref="DisbursementVoucher.Fire"/>); this service only coordinates the
/// transaction, numbering and persistence.
/// </summary>
public sealed class DisbursementVoucherService(
    IDisbursementVoucherRepository vouchers,
    IFundingSourceCatalog fundingSources,
    IVoucherNumberGenerator numbers,
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
            };

            await vouchers.AddAsync(voucher, token);
            return DvStateView.From(voucher);
        }, cancellationToken);

    public async Task<DvStateView> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var voucher = await vouchers.FindAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"DV '{name}' not found.");
        return DvStateView.From(voucher);
    }

    public async Task<DvStateView> FireAsync(
        string name, DvAction action, TransitionContext context, CancellationToken cancellationToken = default)
    {
        var voucher = await vouchers.FindAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"DV '{name}' not found.");

        voucher.Fire(action, context);

        await vouchers.UpdateAsync(voucher, cancellationToken);
        return DvStateView.From(voucher);
    }
}
