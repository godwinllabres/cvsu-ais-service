using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Domain.Common;
using CvSU.Ais.Domain.Ledgers;

namespace CvSU.Ais.Application.JournalEntries;

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record JeLineDto(
    string Account,
    string? AccountName,
    decimal Debit,
    decimal Credit,
    string? Description);

public sealed record JournalEntryView(
    string Name,
    DateOnly PostingDate,
    string JeType,
    string ApprovalStatus,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record JournalEntryDetailView(
    string Name,
    string? Title,
    DateOnly PostingDate,
    int FiscalYear,
    string? FundCluster,
    string JeType,
    string ApprovalStatus,
    decimal TotalDebit,
    decimal TotalCredit,
    string? ApprovedBy,
    string? GlPostingReference,
    string? UserRemark,
    IReadOnlyList<JeLineDto> Lines);

public sealed record CreateJournalEntryCommand(
    string? Title,
    DateOnly PostingDate,
    int FiscalYear,
    string? FundCluster,
    string JeType,
    IReadOnlyList<JeLineDto> Lines,
    string? UserRemark);

// ── Service ─────────────────────────────────────────────────────────────────

public sealed class JournalEntryService(
    IJournalEntryRepository repo,
    IGeneralLedger generalLedger,
    IUnitOfWork unitOfWork)
{
    public async Task<JournalEntryView> CreateAsync(
        CreateJournalEntryCommand command,
        CancellationToken ct = default)
    {
        if (!command.Lines.Any())
            throw new InvalidOperationException("Journal entry must have at least one line.");

        if (command.Lines.Any(l => l.Debit < 0 || l.Credit < 0))
            throw new InvalidOperationException("JE line amounts cannot be negative.");

        if (command.Lines.Any(l => l.Debit > 0 && l.Credit > 0))
            throw new InvalidOperationException(
                "A JE line cannot have both debit and credit amounts; use separate lines.");

        if (command.Lines.Any(l => l.Debit == 0 && l.Credit == 0))
            throw new InvalidOperationException("Each JE line must have a non-zero debit or credit.");

        var totalDebit = command.Lines.Sum(l => l.Debit);
        var totalCredit = command.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                $"Journal entry is not balanced: debits {totalDebit:N2} ≠ credits {totalCredit:N2}.");

        return await repo.AddAsync(command, ct);
    }

    public Task<IReadOnlyList<JournalEntryView>> ListAsync(CancellationToken ct = default) =>
        repo.ListAsync(ct);

    public async Task<JournalEntryDetailView> GetAsync(string name, CancellationToken ct = default)
    {
        var detail = await repo.GetAsync(name, ct);
        return detail ?? throw new KeyNotFoundException($"Journal entry '{name}' was not found.");
    }

    public Task ApproveAsync(string name, string approvedBy, CancellationToken ct = default) =>
        repo.UpdateStatusAsync(name, "Approved", approvedBy, ct);

    /// <summary>
    /// Posts the approved JE to the accrual GL. Builds one <see cref="GeneralLedgerEntry"/>
    /// per je_line, verifies balance, appends the batch, stamps the GL reference, and
    /// transitions status to Posted — all inside one database transaction.
    /// </summary>
    public Task PostAsync(string name, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync<int>(async token =>
        {
            var detail = await repo.GetAsync(name, token)
                ?? throw new KeyNotFoundException($"Journal entry '{name}' was not found.");

            if (detail.ApprovalStatus != "Approved")
                throw new InvalidOperationException(
                    $"Journal entry '{name}' must be Approved before posting (current: {detail.ApprovalStatus}).");

            var batch = new GlPostingBatch();
            foreach (var line in detail.Lines)
            {
                batch.Add(new GeneralLedgerEntry(
                    detail.PostingDate,
                    detail.FiscalYear,
                    line.Account,
                    new Money(line.Debit),
                    new Money(line.Credit),
                    "Journal Entry Voucher",
                    name,
                    line.Description));
            }
            batch.EnsureBalanced();

            await generalLedger.AppendBatchAsync(batch, token);
            await repo.SetGlReferenceAsync(name, name, token);
            await repo.UpdateStatusAsync(name, "Posted", null, token);
            return 0;
        }, ct);

    public Task CancelAsync(string name, CancellationToken ct = default) =>
        repo.UpdateStatusAsync(name, "Cancelled", null, ct);
}
