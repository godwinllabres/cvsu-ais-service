using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Contracts;
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
    decimal TaxWithheld = 0,
    DvType DvType = DvType.Suppliers);

/// <summary>Inputs to re-encode a Draft DV. The fiscal year is fixed by the DV number,
/// so it is not editable; everything else in the encode phase is. Certifications are
/// excluded — they are asserted by their responsible officers via the certify action.</summary>
public sealed record UpdateDvCommand(
    decimal Amount,
    string FundingSourceCode,
    DvType DvType,
    string? PapCode = null,
    string? LocationCode = null,
    ExpenseClass? ExpenseClass = null,
    string? ObjectAccountCode = null,
    decimal TaxWithheld = 0);

/// <summary>Maps the DV aggregate onto the shared wire DTOs (CvSU.Ais.Contracts).
/// The DTOs carry strongly-typed Status/ExpenseClass; the API's JsonStringEnumConverter
/// serialises them as the same strings the client deserialises back into the enums.</summary>
internal static class DvViews
{
    public static DvStateView ToStateView(DisbursementVoucher dv) => new(
        dv.Name, dv.Lifecycle.ToString(), dv.Status, dv.FundCluster.Code,
        dv.ApprovedBy, dv.ApprovedForPaymentBy, dv.ControlNumber);

    public static DvDetailView ToDetailView(DisbursementVoucher dv) => new(
        dv.Name,
        FiscalYearFromName(dv.Name),
        dv.Encoder,
        dv.Amount.Amount,
        dv.Lifecycle.ToString(),
        dv.Status,
        dv.DvType,
        dv.FundingSource.Code,
        dv.FundCluster.Code,
        dv.FundCluster.Name,
        dv.PapCode,
        dv.LocationCode,
        dv.ExpenseClass,
        dv.ObjectAccountCode,
        dv.TaxWithheld.Amount,
        dv.NetAmountPayable.Amount,
        ToCertView(dv, Certification.BudgetSufficiency),
        ToCertView(dv, Certification.InternalAudit),
        ToCertView(dv, Certification.EndUserAcceptance),
        ToCertView(dv, Certification.AccountantSignature),
        ToCertView(dv, Certification.SupplyPropertyInspection),
        dv.ApprovedBy,
        dv.ApprovedForPaymentBy,
        dv.ControlNumber,
        dv.PaymentMethod,
        dv.PaymentReference);

    private static CertificationView ToCertView(DisbursementVoucher dv, Certification certification)
    {
        var c = dv.CertificationOf(certification);
        return new CertificationView(c.Done, c.By, c.At);
    }

    /// <summary>The fiscal year lives in the gapless DV number (<c>DV-YYYY-#####</c>);
    /// parse it back for display rather than carrying a duplicate column.</summary>
    internal static int FiscalYearFromName(string name)
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

            var voucher = new DisbursementVoucher(
                name, command.Encoder, new Money(command.Amount), fundingSource, command.DvType)
            {
                PapCode = command.PapCode,
                LocationCode = command.LocationCode,
                ExpenseClass = command.ExpenseClass,
                ObjectAccountCode = command.ObjectAccountCode,
                TaxWithheld = new Money(command.TaxWithheld),
            };

            await vouchers.AddAsync(voucher, token);
            return DvViews.ToStateView(voucher);
        }, cancellationToken);

    public Task<IReadOnlyList<DvStateView>> ListAsync(CancellationToken cancellationToken = default) =>
        vouchers.ListAsync(cancellationToken);

    public async Task<DvDetailView> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var voucher = await vouchers.FindAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"DV '{name}' not found.");
        return DvViews.ToDetailView(voucher);
    }

    /// <summary>
    /// Re-encode a Draft DV (correct the amount, budget line, certifications, etc.).
    /// The aggregate rejects edits once the DV has left Draft, so encoding stays
    /// editable up to — and only up to — the encoding-complete gate.
    /// </summary>
    public Task<DvDetailView> UpdateAsync(
        string name, UpdateDvCommand command, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var voucher = await vouchers.FindAsync(name, token)
                ?? throw new KeyNotFoundException($"DV '{name}' not found.");
            var fundingSource = await fundingSources.FindAsync(command.FundingSourceCode, token)
                ?? throw new KeyNotFoundException($"Unknown funding source '{command.FundingSourceCode}'.");

            voucher.UpdateEncoding(
                new Money(command.Amount), new Money(command.TaxWithheld), fundingSource, command.DvType,
                command.PapCode, command.LocationCode, command.ExpenseClass, command.ObjectAccountCode);

            await vouchers.UpdateAsync(voucher, token);
            return DvViews.ToDetailView(voucher);
        }, cancellationToken);

    /// <summary>
    /// Record a certification on a DV. The aggregate enforces that the caller holds the
    /// role responsible for that certification and that the DV is still pre-approval; the
    /// certifier's identity and the timestamp are stamped as the audit trail.
    /// </summary>
    public Task<DvDetailView> CertifyAsync(
        string name, Certification certification, TransitionContext context,
        CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var voucher = await vouchers.FindAsync(name, token)
                ?? throw new KeyNotFoundException($"DV '{name}' not found.");

            voucher.Certify(certification, context, DateTime.UtcNow);

            await vouchers.UpdateAsync(voucher, token);
            return DvViews.ToDetailView(voucher);
        }, cancellationToken);

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
                case DvAction.Approve:
                    // Stamp the gapless control number at approval — the docstatus 0→1
                    // boundary. The series is per fund cluster per fiscal year, and the
                    // generator increments inside this transaction so a rollback un-burns it.
                    var series = $"DV-CN-{voucher.FundCluster.Code}-{DvViews.FiscalYearFromName(voucher.Name)}";
                    voucher.AssignControlNumber(await numbers.NextAsync(series, token));
                    break;
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
            return DvViews.ToStateView(voucher);
        }, cancellationToken);

    /// <summary>
    /// Record how a DV is paid (cheque/ADA/transfer). The repository enforces that
    /// the reference is not already used by another DV — the duplicate-disbursement
    /// guard — before the aggregate stores it, all inside one transaction.
    /// </summary>
    public Task<DvStateView> RecordPaymentAsync(
        string name, DvPaymentMethod method, string reference, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var voucher = await vouchers.FindAsync(name, token)
                ?? throw new KeyNotFoundException($"DV '{name}' not found.");

            var trimmed = (reference ?? string.Empty).Trim();
            if (trimmed.Length > 0 && await vouchers.PaymentReferenceExistsAsync(method, trimmed, name, token))
                throw new DuplicatePaymentIdentifierException(
                    $"Payment reference '{trimmed}' ({method}) is already used by another DV. " +
                    "Each cheque/ADA/transfer reference may identify only one disbursement.");

            voucher.RecordPayment(method, trimmed);
            await vouchers.UpdateAsync(voucher, token);
            return DvViews.ToStateView(voucher);
        }, cancellationToken);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
