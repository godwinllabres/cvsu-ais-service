using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            row.Name, row.Encoder, new Money(row.Amount), fundingSource, row.DvType,
            Enum.Parse<DvLifecycleState>(row.Lifecycle),
            Enum.Parse<DvWorkflowStatus>(row.Status),
            row.ApprovedBy, row.ApprovedForPaymentBy,
            new CertificationState(row.BudgetCertified, row.BudgetCertifiedBy, row.BudgetCertifiedAt),
            new CertificationState(row.InternalAuditConfirmed, row.InternalAuditConfirmedBy, row.InternalAuditConfirmedAt),
            new CertificationState(row.EndUserConfirmed, row.EndUserConfirmedBy, row.EndUserConfirmedAt),
            new CertificationState(row.AccountantSigned, row.AccountantSignedBy, row.AccountantSignedAt),
            new CertificationState(row.SupplyPropertySignedOff, row.SupplyPropertySignedOffBy, row.SupplyPropertySignedOffAt),
            row.PapCode, row.LocationCode, row.ExpenseClass, row.ObjectAccountCode,
            row.ControlNumber, row.PaymentMethod, row.PaymentReference,
            taxWithheld: new Money(row.TaxWithheld));
    }

    public async Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Project the raw columns in SQL, then map to the DTO in memory: the stored
        // Status string must be parsed to the DvWorkflowStatus enum, which EF Core
        // cannot translate inside the query.
        var rows = await db.Set<DisbursementVoucherRow>()
            .Join(db.Set<FundingSourceRow>(),
                dv => dv.FundingSourceCode, fs => fs.Code,
                (dv, fs) => new { Dv = dv, fs.ClusterCode })
            .OrderBy(x => x.Dv.Name)
            .Select(x => new
            {
                x.Dv.Name,
                x.Dv.Lifecycle,
                x.Dv.Status,
                x.ClusterCode,
                x.Dv.ApprovedBy,
                x.Dv.ApprovedForPaymentBy,
                x.Dv.ControlNumber,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DvStateView(
            r.Name, r.Lifecycle, Enum.Parse<DvWorkflowStatus>(r.Status), r.ClusterCode,
            r.ApprovedBy, r.ApprovedForPaymentBy, r.ControlNumber)).ToList();
    }

    public async Task AddAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default)
    {
        db.Add(ToRow(voucher));
        await SaveAsync(cancellationToken);
    }

    public async Task UpdateAsync(DisbursementVoucher voucher, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<DisbursementVoucherRow>()
            .FirstOrDefaultAsync(r => r.Name == voucher.Name, cancellationToken)
            ?? throw new InvalidOperationException($"DV {voucher.Name} not found for update.");

        CopyState(voucher, row);
        await SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Persist, translating a Postgres unique-violation (23505) on the control-number or
    /// payment-reference partial indexes into the matching domain exception (→ 422). This
    /// is the backstop the application pre-checks cannot cover under READ COMMITTED, where
    /// two concurrent writers can both pass the pre-check before either commits.
    /// </summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                           && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if (pg.ConstraintName == "ix_disbursement_voucher_payment_method_payment_reference")
                throw new DuplicatePaymentIdentifierException(
                    "This payment reference is already used by another DV. " +
                    "Each cheque/ADA/transfer reference may identify only one disbursement.");
            if (pg.ConstraintName == "ix_disbursement_voucher_control_number")
                throw new DuplicateControlNumberException(
                    "A control number collision was detected; please retry the approval.");
            throw;
        }
    }

    public Task<bool> PaymentReferenceExistsAsync(
        DvPaymentMethod method, string reference, string excludeName, CancellationToken cancellationToken = default) =>
        db.Set<DisbursementVoucherRow>()
            .AnyAsync(
                r => r.PaymentMethod == method && r.PaymentReference == reference && r.Name != excludeName,
                cancellationToken);

    private static DisbursementVoucherRow ToRow(DisbursementVoucher dv) => new()
    {
        Name = dv.Name,
        Encoder = dv.Encoder,
        Amount = dv.Amount.Amount,
        TaxWithheld = dv.TaxWithheld.Amount,
        DvType = dv.DvType,
        FundingSourceCode = dv.FundingSource.Code,
        Lifecycle = dv.Lifecycle.ToString(),
        Status = dv.Status.ToString(),
        ApprovedBy = dv.ApprovedBy,
        ApprovedForPaymentBy = dv.ApprovedForPaymentBy,
        BudgetCertified = dv.BudgetCertified,
        BudgetCertifiedBy = dv.BudgetCertifiedBy,
        BudgetCertifiedAt = dv.BudgetCertifiedAt,
        InternalAuditConfirmed = dv.InternalAuditConfirmed,
        InternalAuditConfirmedBy = dv.InternalAuditConfirmedBy,
        InternalAuditConfirmedAt = dv.InternalAuditConfirmedAt,
        EndUserConfirmed = dv.EndUserConfirmed,
        EndUserConfirmedBy = dv.EndUserConfirmedBy,
        EndUserConfirmedAt = dv.EndUserConfirmedAt,
        AccountantSigned = dv.AccountantSigned,
        AccountantSignedBy = dv.AccountantSignedBy,
        AccountantSignedAt = dv.AccountantSignedAt,
        SupplyPropertySignedOff = dv.SupplyPropertySignedOff,
        SupplyPropertySignedOffBy = dv.SupplyPropertySignedOffBy,
        SupplyPropertySignedOffAt = dv.SupplyPropertySignedOffAt,
        PapCode = dv.PapCode,
        LocationCode = dv.LocationCode,
        ExpenseClass = dv.ExpenseClass,
        ObjectAccountCode = dv.ObjectAccountCode,
        ControlNumber = dv.ControlNumber,
        PaymentMethod = dv.PaymentMethod,
        PaymentReference = dv.PaymentReference,
    };

    private static void CopyState(DisbursementVoucher dv, DisbursementVoucherRow row)
    {
        row.Status = dv.Status.ToString();
        row.Lifecycle = dv.Lifecycle.ToString();
        row.ApprovedBy = dv.ApprovedBy;
        row.ApprovedForPaymentBy = dv.ApprovedForPaymentBy;
        // Editable while Draft (UpdateEncoding), so re-copy on every persist.
        row.Amount = dv.Amount.Amount;
        row.FundingSourceCode = dv.FundingSource.Code;
        row.DvType = dv.DvType;
        row.TaxWithheld = dv.TaxWithheld.Amount;
        row.BudgetCertified = dv.BudgetCertified;
        row.BudgetCertifiedBy = dv.BudgetCertifiedBy;
        row.BudgetCertifiedAt = dv.BudgetCertifiedAt;
        row.InternalAuditConfirmed = dv.InternalAuditConfirmed;
        row.InternalAuditConfirmedBy = dv.InternalAuditConfirmedBy;
        row.InternalAuditConfirmedAt = dv.InternalAuditConfirmedAt;
        row.EndUserConfirmed = dv.EndUserConfirmed;
        row.EndUserConfirmedBy = dv.EndUserConfirmedBy;
        row.EndUserConfirmedAt = dv.EndUserConfirmedAt;
        row.AccountantSigned = dv.AccountantSigned;
        row.AccountantSignedBy = dv.AccountantSignedBy;
        row.AccountantSignedAt = dv.AccountantSignedAt;
        row.SupplyPropertySignedOff = dv.SupplyPropertySignedOff;
        row.SupplyPropertySignedOffBy = dv.SupplyPropertySignedOffBy;
        row.SupplyPropertySignedOffAt = dv.SupplyPropertySignedOffAt;
        row.PapCode = dv.PapCode;
        row.LocationCode = dv.LocationCode;
        row.ExpenseClass = dv.ExpenseClass;
        row.ObjectAccountCode = dv.ObjectAccountCode;
        row.ControlNumber = dv.ControlNumber;
        row.PaymentMethod = dv.PaymentMethod;
        row.PaymentReference = dv.PaymentReference;
    }
}
