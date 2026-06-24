using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Funds;

namespace CvSU.Ais.Application.Budget;

public sealed record CreateAppropriationCommand(
    int FiscalYear,
    string FundingSourceCode,
    string PapCode,
    string LocationCode,
    ExpenseClass ExpenseClass,
    string ObjectAccountCode,
    decimal FinalAppropriation);

public sealed record AppropriationView(
    string Id, decimal FinalAppropriation, decimal Allotted, decimal Unallotted);

public sealed record AllotmentView(
    string Id, string AppropriationId, decimal Amount, decimal Obligated, decimal Unobligated);

public sealed record ObligationView(
    string Id, string AllotmentId, decimal Amount, decimal AllotmentUnobligatedBalance);

/// <summary>
/// Drives the budget execution cycle. Each step posts to the budget ledger only
/// (never the accrual GL — obligations are memorandum entries) and reuses the
/// domain aggregates for the ceiling rules. The Allot/Obligate steps run inside
/// a transaction whose <c>Lock*</c> read holds a row lock until commit, so two
/// concurrent callers cannot both pass a ceiling check on stale balances.
/// </summary>
public sealed class BudgetExecutionService(
    IBudgetLedger budgetLedger,
    IFundingSourceCatalog fundingSources,
    IVoucherNumberGenerator numbers,
    IUnitOfWork unitOfWork)
{
    public Task<AppropriationView> CreateAppropriationAsync(
        CreateAppropriationCommand command, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var fundingSource = await fundingSources.FindAsync(command.FundingSourceCode, token)
                ?? throw new KeyNotFoundException($"Unknown funding source '{command.FundingSourceCode}'.");

            var uacs = new UacsCode(
                fundingSource, command.PapCode, command.LocationCode,
                command.ExpenseClass, command.ObjectAccountCode);

            var id = await numbers.NextAsync($"APP-{command.FiscalYear}", token);
            var appropriation = new Appropriation(
                id, command.FiscalYear, uacs, new Money(command.FinalAppropriation));

            await budgetLedger.AppendAsync(appropriation.CreatePosting(Today), token);

            return new AppropriationView(
                id, appropriation.FinalAppropriation.Amount,
                appropriation.AllottedAmount.Amount, appropriation.UnallottedBalance.Amount);
        }, cancellationToken);

    public Task<AllotmentView> AllotAsync(
        string appropriationId, decimal amount, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var snapshot = await budgetLedger.LockAppropriationAsync(appropriationId, token)
                ?? throw new KeyNotFoundException($"Appropriation '{appropriationId}' not found.");

            var appropriation = Appropriation.Rehydrate(
                snapshot.Id, snapshot.FiscalYear, snapshot.Uacs, snapshot.FinalAppropriation, snapshot.Allotted);

            var allotmentId = await numbers.NextAsync($"ALL-{snapshot.FiscalYear}", token);
            var allotment = appropriation.Allot(allotmentId, new Money(amount), Today); // R-BUD-01

            await budgetLedger.AppendAsync(allotment.CreatePosting(Today), token);

            return new AllotmentView(
                allotmentId, appropriationId, allotment.Amount.Amount,
                allotment.ObligatedAmount.Amount, allotment.UnobligatedBalance.Amount);
        }, cancellationToken);

    public Task<ObligationView> ObligateAsync(
        string allotmentId, decimal amount, CancellationToken cancellationToken = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var snapshot = await budgetLedger.LockAllotmentAsync(allotmentId, token)
                ?? throw new KeyNotFoundException($"Allotment '{allotmentId}' not found.");

            var appropriation = Appropriation.Rehydrate(
                snapshot.Appropriation.Id, snapshot.Appropriation.FiscalYear, snapshot.Appropriation.Uacs,
                snapshot.Appropriation.FinalAppropriation, snapshot.Appropriation.Allotted);
            var allotment = Allotment.Rehydrate(
                snapshot.Id, appropriation, snapshot.Amount, snapshot.ReleaseDate, snapshot.Obligated);

            var orsNumber = await numbers.NextAsync($"ORS-{snapshot.Appropriation.FiscalYear}", token);

            // The obligation inherits the allotment's UACS line (same fund/PAP/expense class),
            // so R-BUD-05 / cross-cluster are satisfied by construction; R-BUD-02 is the live gate.
            var entry = allotment.Obligate(orsNumber, new Money(amount), snapshot.Appropriation.Uacs, Today);
            await budgetLedger.AppendAsync(entry, token);

            return new ObligationView(
                orsNumber, allotmentId, amount, allotment.UnobligatedBalance.Amount);
        }, cancellationToken);

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
