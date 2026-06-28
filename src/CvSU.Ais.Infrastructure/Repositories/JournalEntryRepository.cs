using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.JournalEntries;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class JournalEntryRepository(AisDbContext db) : IJournalEntryRepository
{
    public async Task<IReadOnlyList<JournalEntryView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<JournalEntryRow>()
            .OrderByDescending(r => r.PostingDate)
            .ThenBy(r => r.Name)
            .Select(r => new JournalEntryView(
                r.Name,
                r.PostingDate,
                r.JeType,
                r.ApprovalStatus,
                r.TotalDebit,
                r.TotalCredit))
            .ToListAsync(ct);
    }

    public async Task<JournalEntryDetailView?> GetAsync(string name, CancellationToken ct)
    {
        var row = await db.Set<JournalEntryRow>()
            .Include(r => r.JeLines)
            .FirstOrDefaultAsync(r => r.Name == name, ct);

        if (row is null)
            return null;

        return ToDetailView(row);
    }

    public async Task<JournalEntryView> AddAsync(CreateJournalEntryCommand command, CancellationToken ct)
    {
        var name = "JE-" + Guid.NewGuid().ToString("N")[..12];

        var totalDebit = command.Lines.Sum(l => l.Debit);
        var totalCredit = command.Lines.Sum(l => l.Credit);

        var row = new JournalEntryRow
        {
            Name = name,
            Title = command.Title,
            PostingDate = command.PostingDate,
            FiscalYear = command.FiscalYear,
            FundCluster = command.FundCluster,
            JeType = command.JeType,
            ApprovalStatus = "Draft",
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            UserRemark = command.UserRemark,
            JeLines = command.Lines.Select(l => new JeLineRow
            {
                ParentJeName = name,
                Account = l.Account,
                AccountName = l.AccountName,
                Debit = l.Debit,
                Credit = l.Credit,
                Description = l.Description,
            }).ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(ct);

        return new JournalEntryView(
            row.Name,
            row.PostingDate,
            row.JeType,
            row.ApprovalStatus,
            row.TotalDebit,
            row.TotalCredit);
    }

    public async Task UpdateStatusAsync(
        string name,
        string newStatus,
        string? approvedBy,
        CancellationToken ct)
    {
        var row = await db.Set<JournalEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"Journal entry '{name}' was not found.");

        row.ApprovalStatus = newStatus;

        if (approvedBy is not null)
        {
            row.ApprovedBy = approvedBy;
            row.ApprovedOn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task SetGlReferenceAsync(string name, string glRef, CancellationToken ct)
    {
        var row = await db.Set<JournalEntryRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"Journal entry '{name}' was not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(ct);
    }

    private static JournalEntryDetailView ToDetailView(JournalEntryRow row) =>
        new(
            row.Name,
            row.Title,
            row.PostingDate,
            row.FiscalYear,
            row.FundCluster,
            row.JeType,
            row.ApprovalStatus,
            row.TotalDebit,
            row.TotalCredit,
            row.ApprovedBy,
            row.GlPostingReference,
            row.UserRemark,
            row.JeLines
                .Select(l => new JeLineDto(
                    l.Account,
                    l.AccountName,
                    l.Debit,
                    l.Credit,
                    l.Description))
                .ToList());
}
