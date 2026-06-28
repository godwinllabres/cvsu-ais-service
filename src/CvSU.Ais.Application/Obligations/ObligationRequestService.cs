using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Common;

namespace CvSU.Ais.Application.Obligations;

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record OrsLineItemDto(
    string Particulars,
    string? AllotmentId,
    decimal Amount,
    string? PapCode,
    string? LocationCode,
    string? ExpenseClass,
    string? Remarks);

public sealed record OrsView(
    string Name,
    DateOnly PostingDate,
    string RequestingUnit,
    decimal Amount,
    string Status);

public sealed record OrsDetailView(
    string Name,
    DateOnly PostingDate,
    int FiscalYear,
    string RequestingUnit,
    string Purpose,
    decimal Amount,
    string FundingSourceCode,
    string? PapCode,
    string? LocationCode,
    string? ExpenseClass,
    string Status,
    string? RequestingOfficeUser,
    string? BudgetOfficerUser,
    string? Remarks,
    IReadOnlyList<OrsLineItemDto> LineItems);

public sealed record CreateOrsCommand(
    string RequestingUnit,
    DateOnly PostingDate,
    int FiscalYear,
    string Purpose,
    decimal Amount,
    string FundingSourceCode,
    string? PapCode,
    string? LocationCode,
    string? ExpenseClass,
    string? RequestingOfficeUser,
    string? BudgetOfficerUser,
    IReadOnlyList<OrsLineItemDto> LineItems,
    string? Remarks);

public sealed record NcaView(
    string NcaNumber,
    DateOnly DateReceived,
    int FiscalYear,
    string FundingSourceCode,
    DateOnly ValidityDate,
    string Status,
    decimal NcaAmount,
    decimal UtilizedAmount);

public sealed record CreateNcaCommand(
    string NcaNumber,
    DateOnly DateReceived,
    int FiscalYear,
    string FundingSourceCode,
    DateOnly ValidityDate,
    decimal NcaAmount,
    string? Remarks);

// ── Services ─────────────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates ORS/BURS creation and workflow. <see cref="FundVerifyAsync"/> is the
/// critical step: it takes a SELECT FOR UPDATE lock on the target allotment, runs the
/// R-BUD-02 ceiling check via the domain aggregate, posts the Obligation budget entry,
/// and transitions the ORS status — all in a single transaction.
/// </summary>
public sealed class ObligationRequestService(
    IObligationRequestRepository repo,
    IBudgetLedger budgetLedger,
    IUnitOfWork unitOfWork)
{
    public Task<OrsView> CreateAsync(CreateOrsCommand command, CancellationToken ct = default) =>
        repo.AddAsync(command, ct);

    public Task<IReadOnlyList<OrsView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<OrsDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var view = await repo.GetAsync(name, ct);
        return view ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");
    }

    public async Task<OrsView> SubmitAsync(string name, CancellationToken ct = default)
    {
        await repo.UpdateStatusAsync(name, "Submitted", ct);
        return await GetViewOrThrow(name, ct);
    }

    public async Task<OrsView> ReviewAsync(string name, CancellationToken ct = default)
    {
        await repo.UpdateStatusAsync(name, "Reviewed", ct);
        return await GetViewOrThrow(name, ct);
    }

    /// <summary>
    /// Locks the target allotment (SELECT FOR UPDATE), validates the obligation ceiling
    /// (R-BUD-02 via <see cref="Allotment.Obligate"/>), appends the Obligation entry to
    /// the budget registry, and transitions the ORS to FundVerified.
    /// </summary>
    public Task<OrsView> FundVerifyAsync(
        string name, string allotmentId, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");

            if (detail.Status != "Reviewed")
                throw new InvalidOperationException(
                    $"ORS/BURS '{name}' must be Reviewed before fund-verification (current: {detail.Status}).");

            var snapshot = await budgetLedger.LockAllotmentAsync(allotmentId, token)
                ?? throw new KeyNotFoundException(
                    $"Allotment '{allotmentId}' not found. " +
                    "Ensure an allotment has been released against an appropriation for this UACS line.");

            var appropriation = Appropriation.Rehydrate(
                snapshot.Appropriation.Id,
                snapshot.Appropriation.FiscalYear,
                snapshot.Appropriation.Uacs,
                snapshot.Appropriation.FinalAppropriation,
                snapshot.Appropriation.Allotted);

            var allotment = Allotment.Rehydrate(
                snapshot.Id, appropriation, snapshot.Amount, snapshot.ReleaseDate, snapshot.Obligated);

            // Obligate() enforces R-BUD-02 (ceiling), R-BUD-05 (STF/PS prohibition),
            // and fund-cluster integrity. Throws on any violation.
            var obligationEntry = allotment.Obligate(
                name,
                new Money(detail.Amount),
                snapshot.Appropriation.Uacs,
                Today);

            await budgetLedger.AppendAsync(obligationEntry, token);
            await repo.UpdateStatusAsync(name, "FundVerified", token);
            return await GetViewOrThrow(name, token);
        }, ct);

    public async Task<OrsView> SignAsync(string name, CancellationToken ct = default)
    {
        await repo.UpdateStatusAsync(name, "Signed", ct);
        return await GetViewOrThrow(name, ct);
    }

    public async Task<OrsView> CancelAsync(string name, CancellationToken ct = default)
    {
        await repo.UpdateStatusAsync(name, "Cancelled", ct);
        return await GetViewOrThrow(name, ct);
    }

    private async Task<OrsView> GetViewOrThrow(string name, CancellationToken ct)
    {
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");
        return new OrsView(detail.Name, detail.PostingDate, detail.RequestingUnit, detail.Amount, detail.Status);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

/// <summary>CRUD service for Notices of Cash Allocation.
/// NCA is tracked as an available-cash registry entry; GL posting (subsidy from NG)
/// is handled via a manual Journal Entry Voucher by the accountant.</summary>
public sealed class NcaService(INcaRepository repo)
{
    public Task<IReadOnlyList<NcaView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<NcaView> GetAsync(string ncaNumber, CancellationToken ct = default)
    {
        var view = await repo.GetAsync(ncaNumber, ct);
        return view ?? throw new KeyNotFoundException($"NCA '{ncaNumber}' not found.");
    }

    public Task<NcaView> AddAsync(CreateNcaCommand command, CancellationToken ct = default) =>
        repo.AddAsync(command, ct);
}
