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
    /// <summary>
    /// The only legal forward path is Draft → Submitted → Reviewed → FundVerified → Signed.
    /// Cancellation is allowed only from pre-Signed states. Any transition not listed here
    /// is rejected — mirroring the spirit of <c>DvStateMachine</c>, this table is the sole
    /// authority on status changes, so the FundVerified → Reviewed reversal and any move out
    /// of a terminal state (Signed/Cancelled) are structurally impossible.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Draft"] = ["Submitted", "Cancelled"],
            ["Submitted"] = ["Reviewed", "Cancelled"],
            ["Reviewed"] = ["FundVerified", "Cancelled"],
            ["FundVerified"] = ["Signed", "Cancelled"],
            ["Signed"] = [],
            ["Cancelled"] = [],
        };

    /// <summary>Loads the ORS/BURS and asserts <paramref name="target"/> is a legal
    /// transition from its current status, throwing otherwise.</summary>
    private async Task<OrsDetailView> EnsureTransitionAllowed(
        string name, string target, CancellationToken ct)
    {
        var detail = await repo.GetAsync(name, ct)
            ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");

        if (!AllowedTransitions.TryGetValue(detail.Status, out var allowed)
            || !allowed.Contains(target, StringComparer.Ordinal))
            throw new InvalidOperationException(
                $"'{target}' is not a legal transition for ORS/BURS '{name}' from '{detail.Status}'. " +
                $"Legal next states here: {(allowed is { Length: > 0 } ? string.Join(", ", allowed) : "(none — terminal state)")}.");

        return detail;
    }

    public Task<OrsView> CreateAsync(CreateOrsCommand command, CancellationToken ct = default)
    {
        if (command.Amount <= 0m)
            throw new ArgumentException(
                "ORS/BURS amount must be greater than zero.", nameof(command));

        if (command.LineItems is { Count: > 0 })
        {
            var lineTotal = command.LineItems.Sum(li => li.Amount);
            if (Math.Abs(lineTotal - command.Amount) > 0.01m)
                throw new ArgumentException(
                    $"ORS/BURS header amount {command.Amount:N2} does not reconcile with the sum of line "
                    + $"amounts {lineTotal:N2}.", nameof(command));
        }

        return repo.AddAsync(command, ct);
    }

    public Task<IReadOnlyList<OrsView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<OrsDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var view = await repo.GetAsync(name, ct);
        return view ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");
    }

    public async Task<OrsView> SubmitAsync(string name, CancellationToken ct = default)
    {
        await EnsureTransitionAllowed(name, "Submitted", ct);
        await repo.UpdateStatusAsync(name, "Submitted", ct);
        return await GetViewOrThrow(name, ct);
    }

    public async Task<OrsView> ReviewAsync(string name, CancellationToken ct = default)
    {
        await EnsureTransitionAllowed(name, "Reviewed", ct);
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
            // Re-read under the transaction and gate on the transition table. Because the
            // only legal predecessor of FundVerified is Reviewed, an ORS that is already
            // FundVerified or Signed is rejected here — so Allotment.Obligate cannot run twice.
            var detail = await EnsureTransitionAllowed(name, "FundVerified", token);

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
        await EnsureTransitionAllowed(name, "Signed", ct);
        await repo.UpdateStatusAsync(name, "Signed", ct);
        return await GetViewOrThrow(name, ct);
    }

    public async Task<OrsView> CancelAsync(string name, CancellationToken ct = default)
    {
        await EnsureTransitionAllowed(name, "Cancelled", ct);
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
