using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.Collections;

// ---------------------------------------------------------------------------
// DTOs – Order of Payment
// ---------------------------------------------------------------------------

public sealed record OrderOfPaymentView(
    string Name,
    DateOnly OrderDate,
    string Customer,
    decimal Amount,
    string Status);

public sealed record OrderOfPaymentDetailView(
    string Name,
    DateOnly OrderDate,
    string Customer,
    string Description,
    decimal Amount,
    string? FundCluster,
    string Status,
    string? IssuedBy,
    string? Remarks);

public sealed record CreateOrderOfPaymentCommand(
    DateOnly OrderDate,
    string Customer,
    string Description,
    decimal Amount,
    string? FundCluster,
    string? IssuedBy,
    string? Remarks);

// ---------------------------------------------------------------------------
// DTOs – Official Receipt
// ---------------------------------------------------------------------------

public sealed record OfficialReceiptView(
    string Name,
    string OrNumber,
    DateOnly PostingDate,
    string Customer,
    decimal AmountPaid,
    string ModeOfPayment,
    string CollectionStatus);

public sealed record OfficialReceiptDetailView(
    string Name,
    string OrNumber,
    DateOnly PostingDate,
    string? OrderOfPaymentName,
    string Customer,
    decimal AmountPaid,
    string ModeOfPayment,
    string? FundCluster,
    string CollectionStatus,
    string? Remarks);

public sealed record CreateOfficialReceiptCommand(
    string OrNumber,
    DateOnly PostingDate,
    string? OrderOfPaymentName,
    string Customer,
    decimal AmountPaid,
    string ModeOfPayment,
    string? FundCluster,
    string? IncomeAccount,
    string? Remarks);

// ---------------------------------------------------------------------------
// DTOs – Report of Collections and Deposits (RCD)
// ---------------------------------------------------------------------------

public sealed record RcdLineDto(
    string OfficialReceiptName,
    string? OrNumber,
    DateOnly? PostingDate,
    string? Payor,
    string? ModeOfPayment,
    decimal AmountCollected);

public sealed record RcdView(
    string Name,
    DateOnly ReportDate,
    string CollectingOfficer,
    decimal TotalCollected,
    string Status);

public sealed record RcdDetailView(
    string Name,
    DateOnly ReportDate,
    int FiscalYear,
    string? FundCluster,
    string CollectingOfficer,
    string DepositSlipNo,
    DateOnly DepositDate,
    string DepositoryBank,
    string? DepositAccountNumber,
    decimal TotalCollected,
    decimal TotalDeposited,
    string Status,
    string? Remarks,
    IReadOnlyList<RcdLineDto> Lines);

public sealed record CreateRcdCommand(
    DateOnly ReportDate,
    int FiscalYear,
    string? FundCluster,
    string CollectingOfficer,
    string DepositSlipNo,
    DateOnly DepositDate,
    string DepositoryBank,
    string? DepositAccountNumber,
    decimal TotalDeposited,
    IReadOnlyList<RcdLineDto> Lines,
    string? Remarks);

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------

/// <summary>Orchestrates Order of Payment lifecycle: create, list, get, issue, cancel.</summary>
public sealed class OrderOfPaymentService(IOrderOfPaymentRepository repo)
{
    public Task<IReadOnlyList<OrderOfPaymentView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<OrderOfPaymentDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Order of Payment '{name}' not found.");

    public Task<OrderOfPaymentDetailView> CreateAsync(
        CreateOrderOfPaymentCommand command,
        CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    public Task<OrderOfPaymentDetailView> IssueAsync(string name, CancellationToken cancellationToken = default) =>
        repo.UpdateStatusAsync(name, "Issued", cancellationToken);

    public Task<OrderOfPaymentDetailView> CancelAsync(string name, CancellationToken cancellationToken = default) =>
        repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
}

/// <summary>
/// Orchestrates Official Receipt creation and collection workflow. <see cref="CreateAsync"/>
/// posts the collection GL entry immediately: Dr. Cash on Hand (1010101000) /
/// Cr. Service Income (4010301000, or caller-supplied income account).
/// </summary>
public sealed class OfficialReceiptService(
    IOfficialReceiptRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "Official Receipt";

    public Task<IReadOnlyList<OfficialReceiptView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<OfficialReceiptDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Official Receipt '{name}' not found.");

    /// <summary>
    /// Records the collection and posts the GL entry:
    /// Dr. Cash on Hand / Cr. {incomeAccount}. The income account defaults to
    /// <see cref="GlAccounts.ServiceIncome"/> when not supplied.
    /// </summary>
    public Task<OfficialReceiptDetailView> CreateAsync(
        CreateOfficialReceiptCommand command,
        CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var or = await repo.AddAsync(command, token);

            var amount = new Money(command.AmountPaid);
            var today = command.PostingDate;
            var fiscalYear = today.Year;
            var incomeAccount = string.IsNullOrWhiteSpace(command.IncomeAccount)
                ? GlAccounts.ServiceIncome
                : command.IncomeAccount;

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashOnHand, amount, Money.Zero,
                    DocType, or.Name, $"Collection from {command.Customer}"))
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    incomeAccount, Money.Zero, amount,
                    DocType, or.Name, $"Collection from {command.Customer}"));
            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            return or;
        }, cancellationToken);

    public Task<OfficialReceiptDetailView> CloseAsync(string name, CancellationToken cancellationToken = default) =>
        repo.UpdateStatusAsync(name, "Closed", cancellationToken);

    public Task<OfficialReceiptDetailView> CancelAsync(string name, CancellationToken cancellationToken = default) =>
        repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
}

/// <summary>
/// Orchestrates RCD lifecycle. <see cref="DepositAsync"/> validates that the RCD total
/// matches its OR lines and posts the deposit GL entry:
/// Dr. Cash in Bank–LCCA (1010201000) / Cr. Cash on Hand (1010101000).
/// </summary>
public sealed class RcdService(
    IRcdRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    private const string DocType = "Report of Collections";

    public Task<IReadOnlyList<RcdView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public async Task<RcdDetailView> GetAsync(string name, CancellationToken cancellationToken = default) =>
        await repo.GetAsync(name, cancellationToken)
            ?? throw new KeyNotFoundException($"Report of Collections and Deposits '{name}' not found.");

    public Task<RcdDetailView> CreateAsync(
        CreateRcdCommand command,
        CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);

    /// <summary>
    /// Validates that the RCD OR-line total agrees with <c>total_collected</c>, then
    /// posts the deposit GL: Dr. Cash in Bank–LCCA / Cr. Cash on Hand for
    /// <c>total_deposited</c>. Uses the deposit slip amount (not collection total) to
    /// match the physical bank deposit.
    /// </summary>
    public Task<RcdDetailView> DepositAsync(string name, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"RCD '{name}' not found.");

            if (detail.Status != "Draft")
                throw new InvalidOperationException(
                    $"Cannot deposit RCD '{name}' from status '{detail.Status}'; expected Draft.");

            var lineTotal = detail.Lines.Sum(l => l.AmountCollected);
            if (Math.Abs(lineTotal - detail.TotalCollected) > 0.01m)
                throw new InvalidOperationException(
                    $"RCD '{name}' line total {lineTotal:N2} does not match total_collected {detail.TotalCollected:N2}.");

            var depositAmount = new Money(detail.TotalDeposited);
            var today = detail.DepositDate;
            var fiscalYear = today.Year;

            var batch = new GlPostingBatch()
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashInBankLcca, depositAmount, Money.Zero,
                    DocType, name, $"Deposit slip {detail.DepositSlipNo}"))
                .Add(new GeneralLedgerEntry(today, fiscalYear,
                    GlAccounts.CashOnHand, Money.Zero, depositAmount,
                    DocType, name, $"Deposit slip {detail.DepositSlipNo}"));
            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            return await repo.UpdateStatusAsync(name, "Deposited", token);
        }, cancellationToken);

    public Task<RcdDetailView> CancelAsync(string name, CancellationToken cancellationToken = default) =>
        repo.UpdateStatusAsync(name, "Cancelled", cancellationToken);
}
