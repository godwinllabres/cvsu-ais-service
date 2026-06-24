using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class DisbursementVoucherRepository(AisDbContext db, IFundingSourceCatalog fundingSources)
    : IDisbursementVoucherRepository
{
    public async Task<DisbursementVoucher?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<DisbursementVoucherRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (row is null)
            return null;

        var fundingSource = await fundingSources.FindAsync(row.FundingSourceCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DV {name} references unknown funding source {row.FundingSourceCode}.");

        return DisbursementVoucher.Rehydrate(
            row.Name, row.Encoder, new Money(row.Amount), fundingSource,
            Enum.Parse<DvLifecycleState>(row.Lifecycle),
            Enum.Parse<DvWorkflowStatus>(row.Status),
            row.ApprovedBy, row.ApprovedForPaymentBy,
            row.BudgetCertified, row.InternalAuditConfirmed, row.EndUserConfirmed, row.AccountantSigned,
            row.PapCode, row.LocationCode, row.ExpenseClass, row.ObjectAccountCode);
    }

    public async Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<DisbursementVoucherRow>()
            .Join(db.Set<FundingSourceRow>(),
                dv => dv.FundingSourceCode, fs => fs.Code,
                (dv, fs) => new { Dv = dv, fs.ClusterCode })
            .OrderBy(x => x.Dv.Name)
            .Select(x => new DvStateView(
                x.Dv.Name, x.Dv.Lifecycle, x.Dv.Status, x.ClusterCode, x.Dv.ApprovedBy, x.Dv.ApprovedForPaymentBy))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default)
    {
        db.Add(ToRow(voucher));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<DisbursementVoucherRow>()
            .FirstOrDefaultAsync(r => r.Name == voucher.Name, cancellationToken)
            ?? throw new InvalidOperationException($"DV {voucher.Name} not found for update.");

        CopyState(voucher, row);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DisbursementVoucherRow ToRow(DisbursementVoucher dv) => new()
    {
        Name = dv.Name,
        Encoder = dv.Encoder,
        Amount = dv.Amount.Amount,
        FundingSourceCode = dv.FundingSource.Code,
        Lifecycle = dv.Lifecycle.ToString(),
        Status = dv.Status.ToString(),
        ApprovedBy = dv.ApprovedBy,
        ApprovedForPaymentBy = dv.ApprovedForPaymentBy,
        BudgetCertified = dv.BudgetCertified,
        InternalAuditConfirmed = dv.InternalAuditConfirmed,
        EndUserConfirmed = dv.EndUserConfirmed,
        AccountantSigned = dv.AccountantSigned,
        PapCode = dv.PapCode,
        LocationCode = dv.LocationCode,
        ExpenseClass = dv.ExpenseClass,
        ObjectAccountCode = dv.ObjectAccountCode,
    };

    private static void CopyState(DisbursementVoucher dv, DisbursementVoucherRow row)
    {
        row.Status = dv.Status.ToString();
        row.Lifecycle = dv.Lifecycle.ToString();
        row.ApprovedBy = dv.ApprovedBy;
        row.ApprovedForPaymentBy = dv.ApprovedForPaymentBy;
        row.BudgetCertified = dv.BudgetCertified;
        row.InternalAuditConfirmed = dv.InternalAuditConfirmed;
        row.EndUserConfirmed = dv.EndUserConfirmed;
        row.AccountantSigned = dv.AccountantSigned;
        row.PapCode = dv.PapCode;
        row.LocationCode = dv.LocationCode;
        row.ExpenseClass = dv.ExpenseClass;
        row.ObjectAccountCode = dv.ObjectAccountCode;
    }
}
