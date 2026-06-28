using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Exports;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ── FINDES Export ─────────────────────────────────────────────────────────────

public sealed class FindesExportRepository(AisDbContext db) : IFindesExportRepository
{
    public async Task<IReadOnlyList<FindesExportView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<FindesExportRow>()
            .OrderByDescending(r => r.ExportDate)
            .Select(r => new FindesExportView(
                r.Name,
                r.ExportDate,
                r.DvTotalAmount,
                r.ExportTotalAmount,
                r.Variance,
                r.VarianceAcceptable,
                r.ApprovalStatus))
            .ToListAsync(cancellationToken);
    }

    public async Task<FindesExportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<FindesExportRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        var lines = row.Lines
            .Select(l => new FindesExportLineDto(l.DvReference))
            .ToList();

        return new FindesExportDetailView(
            row.Name,
            row.ExportBatch,
            row.ExportDate,
            row.DvTotalAmount,
            row.ExportTotalAmount,
            row.Variance,
            row.VarianceAcceptable,
            row.ApprovalStatus,
            row.ReviewedBy,
            row.ReviewedOn,
            row.GeneratedBy,
            row.GeneratedOn,
            row.Remarks,
            lines);
    }

    public async Task AddAsync(FindesExportDetailView detail, CancellationToken cancellationToken = default)
    {
        var row = new FindesExportRow
        {
            Name = detail.Name,
            ExportBatch = detail.ExportBatch,
            ExportDate = detail.ExportDate,
            DvTotalAmount = detail.DvTotalAmount,
            ExportTotalAmount = detail.ExportTotalAmount,
            Variance = detail.Variance,
            VarianceAcceptable = detail.VarianceAcceptable,
            ApprovalStatus = detail.ApprovalStatus,
            ReviewedBy = detail.ReviewedBy,
            ReviewedOn = detail.ReviewedOn,
            GeneratedBy = detail.GeneratedBy,
            GeneratedOn = detail.GeneratedOn,
            Remarks = detail.Remarks,
            Lines = detail.Lines
                .Select(l => new FindesExportLineRow { DvReference = l.DvReference })
                .ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        string name,
        string status,
        string? reviewedBy,
        DateTime? reviewedOn,
        string? generatedBy,
        DateTime? generatedOn,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<FindesExportRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"FINDES export '{name}' not found.");

        row.ApprovalStatus = status;
        row.ReviewedBy = reviewedBy ?? row.ReviewedBy;
        row.ReviewedOn = reviewedOn ?? row.ReviewedOn;
        row.GeneratedBy = generatedBy ?? row.GeneratedBy;
        row.GeneratedOn = generatedOn ?? row.GeneratedOn;

        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Bank Collection Report ────────────────────────────────────────────────────

public sealed class BankCollectionReportRepository(AisDbContext db) : IBankCollectionReportRepository
{
    public async Task<IReadOnlyList<BankCollectionReportView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<BankCollectionReportRow>()
            .OrderByDescending(r => r.ReportDate)
            .Select(r => new BankCollectionReportView(
                r.Name,
                r.ReportDate,
                r.ReconciliationStatus,
                r.TotalAmount,
                r.ExceptionsCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<BankCollectionReportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<BankCollectionReportRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        var lines = row.Lines
            .Select(l => new BankCollectionLineDto(
                l.RefNo, l.LbpRefNo, l.Amount, l.IsMatched, l.MatchedOrName, l.Remarks))
            .ToList();

        return new BankCollectionReportDetailView(
            row.Name,
            row.ReportDate,
            row.ReconciliationStatus,
            row.TotalLines,
            row.TotalAmount,
            row.ExceptionsCount,
            row.Remarks,
            lines);
    }

    public async Task AddAsync(BankCollectionReportDetailView detail, CancellationToken cancellationToken = default)
    {
        var row = new BankCollectionReportRow
        {
            Name = detail.Name,
            ReportDate = detail.ReportDate,
            ReconciliationStatus = detail.ReconciliationStatus,
            TotalLines = detail.TotalLines,
            TotalAmount = detail.TotalAmount,
            ExceptionsCount = detail.ExceptionsCount,
            Remarks = detail.Remarks,
            Lines = detail.Lines
                .Select(l => new BankCollectionLineRow
                {
                    RefNo = l.RefNo,
                    LbpRefNo = l.LbpRefNo,
                    Amount = l.Amount,
                    IsMatched = l.IsMatched,
                    MatchedOrName = l.MatchedOrName,
                    Remarks = l.Remarks,
                })
                .ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<BankCollectionReportRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Bank collection report '{name}' not found.");

        row.ReconciliationStatus = status;
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Push Token ────────────────────────────────────────────────────────────────

public sealed class PushTokenRepository(AisDbContext db) : IPushTokenRepository
{
    public async Task<IReadOnlyList<PushTokenView>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await db.Set<PushTokenRow>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RegisteredOn)
            .Select(r => new PushTokenView(r.Id, r.UserId, r.Platform, r.IsActive, r.RegisteredOn))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RegisterPushTokenCommand command, CancellationToken cancellationToken = default)
    {
        // Deactivate any existing token with the same value to avoid duplicates.
        var existing = await db.Set<PushTokenRow>()
            .Where(r => r.Token == command.Token && r.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var old in existing)
            old.IsActive = false;

        db.Add(new PushTokenRow
        {
            UserId = command.UserId,
            Token = command.Token,
            Platform = command.Platform,
            IsActive = true,
            RegisteredOn = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<PushTokenRow>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Push token {id} not found.");

        row.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }
}
