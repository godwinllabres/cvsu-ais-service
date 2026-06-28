using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Payments;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ─── LDDAP-ADA Repository ────────────────────────────────────────────────────

public sealed class LddapAdaRepository(AisDbContext db) : ILddapAdaRepository
{
    public async Task<IReadOnlyList<LddapAdaView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<LddapAdaRow>()
            .OrderByDescending(r => r.Name)
            .Select(r => new LddapAdaView(
                r.Name,
                r.PeriodFrom,
                r.PeriodTo,
                r.ApprovalStatus,
                r.TotalAmount,
                r.TotalPayees))
            .ToListAsync(ct);
    }

    public async Task<LddapAdaDetailView?> GetAsync(string name, CancellationToken ct)
    {
        var row = await db.Set<LddapAdaRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct);

        if (row is null)
            return null;

        var items = await db.Set<LddapAdaItemRow>()
            .Where(i => i.ParentLddapName == name)
            .OrderBy(i => i.Id)
            .Select(i => new LddapAdaItemDto(
                i.DvReference,
                i.PayeeName,
                i.PayeeAccountNumber,
                i.BankName,
                i.NetAmount))
            .ToListAsync(ct);

        return new LddapAdaDetailView(
            row.Name,
            row.PeriodFrom,
            row.PeriodTo,
            row.FundCluster,
            row.BankName,
            row.BankAccountNumber,
            row.TotalAmount,
            row.TotalPayees,
            row.ApprovalStatus,
            row.TransmittedDate,
            row.Remarks,
            items);
    }

    public async Task<LddapAdaView> AddAsync(CreateLddapAdaCommand cmd, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var shortId = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var series = $"LDDAP-{today:yyyyMM}";

        var counter = await db.Set<VoucherCounter>()
            .Where(c => c.Series == series)
            .FirstOrDefaultAsync(ct);

        if (counter is null)
        {
            counter = new VoucherCounter { Series = series, Current = 0 };
            db.Add(counter);
        }

        counter.Current++;
        var name = $"{series}-{counter.Current:D5}";

        var totalAmount = cmd.Items.Sum(i => i.NetAmount);

        var row = new LddapAdaRow
        {
            Name = name,
            PeriodFrom = cmd.PeriodFrom,
            PeriodTo = cmd.PeriodTo,
            FundCluster = cmd.FundCluster,
            BankName = cmd.BankName,
            BankAccountNumber = cmd.BankAccountNumber,
            TotalAmount = totalAmount,
            TotalPayees = cmd.Items.Count,
            ApprovalStatus = "Draft",
            TransmittedDate = null,
            Remarks = cmd.Remarks,
        };

        db.Add(row);

        foreach (var item in cmd.Items)
        {
            db.Add(new LddapAdaItemRow
            {
                ParentLddapName = name,
                DvReference = item.DvReference,
                PayeeName = item.PayeeName,
                PayeeAccountNumber = item.PayeeAccountNumber,
                BankName = item.BankName,
                NetAmount = item.NetAmount,
            });
        }

        await db.SaveChangesAsync(ct);

        return new LddapAdaView(
            row.Name,
            row.PeriodFrom,
            row.PeriodTo,
            row.ApprovalStatus,
            row.TotalAmount,
            row.TotalPayees);
    }

    public async Task UpdateStatusAsync(string name, string status, DateOnly? transmittedDate, CancellationToken ct)
    {
        var row = await db.Set<LddapAdaRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"LDDAP-ADA '{name}' not found.");

        row.ApprovalStatus = status;
        if (transmittedDate.HasValue)
            row.TransmittedDate = transmittedDate;

        await db.SaveChangesAsync(ct);
    }
}

// ─── DV Transmittal Repository ───────────────────────────────────────────────

public sealed class DvTransmittalRepository(AisDbContext db) : IDvTransmittalRepository
{
    public async Task<IReadOnlyList<DvTransmittalView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<DvTransmittalRow>()
            .OrderByDescending(r => r.Name)
            .Select(r => new DvTransmittalView(
                r.Name,
                r.TransmittalDate,
                r.TransmittingOfficer,
                r.Status,
                r.TotalAmount))
            .ToListAsync(ct);
    }

    public async Task<DvTransmittalDetailView?> GetAsync(string name, CancellationToken ct)
    {
        var row = await db.Set<DvTransmittalRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct);

        if (row is null)
            return null;

        var items = await db.Set<DvTransmittalItemRow>()
            .Where(i => i.ParentTransmittalName == name)
            .OrderBy(i => i.Id)
            .Select(i => new DvTransmittalItemDto(
                i.DvReference,
                i.DvAmount,
                i.Remarks))
            .ToListAsync(ct);

        return new DvTransmittalDetailView(
            row.Name,
            row.TransmittalDate,
            row.TransmittingOfficer,
            row.ReceivingCashier,
            row.AccountantName,
            row.AccountantSignatureConfirmed,
            row.TotalAmount,
            row.TotalDvCount,
            row.Status,
            row.ReceivedByCashier,
            row.ReceivedDate,
            row.Remarks,
            items);
    }

    public async Task<DvTransmittalView> AddAsync(CreateDvTransmittalCommand cmd, CancellationToken ct)
    {
        var year = cmd.TransmittalDate.Year;
        var series = $"TRANS-{year}";

        var counter = await db.Set<VoucherCounter>()
            .Where(c => c.Series == series)
            .FirstOrDefaultAsync(ct);

        if (counter is null)
        {
            counter = new VoucherCounter { Series = series, Current = 0 };
            db.Add(counter);
        }

        counter.Current++;
        var name = $"{series}-{counter.Current:D5}";

        var totalAmount = cmd.Items.Sum(i => i.DvAmount);

        var row = new DvTransmittalRow
        {
            Name = name,
            TransmittalDate = cmd.TransmittalDate,
            TransmittingOfficer = cmd.TransmittingOfficer,
            ReceivingCashier = cmd.ReceivingCashier,
            AccountantName = null,
            AccountantSignatureConfirmed = false,
            TotalAmount = totalAmount,
            TotalDvCount = cmd.Items.Count,
            Status = "Draft",
            ReceivedByCashier = null,
            ReceivedDate = null,
            Remarks = cmd.Remarks,
        };

        db.Add(row);

        foreach (var item in cmd.Items)
        {
            db.Add(new DvTransmittalItemRow
            {
                ParentTransmittalName = name,
                DvReference = item.DvReference,
                DvAmount = item.DvAmount,
                Remarks = item.Remarks,
            });
        }

        await db.SaveChangesAsync(ct);

        return new DvTransmittalView(
            row.Name,
            row.TransmittalDate,
            row.TransmittingOfficer,
            row.Status,
            row.TotalAmount);
    }

    public async Task UpdateStatusAsync(string name, string status, string? receivedBy, DateOnly? receivedDate, CancellationToken ct)
    {
        var row = await db.Set<DvTransmittalRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"DV Transmittal '{name}' not found.");

        row.Status = status;
        if (receivedBy is not null)
            row.ReceivedByCashier = receivedBy;
        if (receivedDate.HasValue)
            row.ReceivedDate = receivedDate;

        await db.SaveChangesAsync(ct);
    }
}

// ─── Audit Intake Repository ─────────────────────────────────────────────────

public sealed class AuditIntakeRepository(AisDbContext db) : IAuditIntakeRepository
{
    public async Task<IReadOnlyList<AuditIntakeView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<AuditIntakeRow>()
            .OrderByDescending(r => r.Name)
            .Select(r => new AuditIntakeView(
                r.Name,
                r.DisbursementVoucherName,
                r.Status,
                r.AuditResult))
            .ToListAsync(ct);
    }

    public async Task<AuditIntakeDetailView?> GetAsync(string name, CancellationToken ct)
    {
        var row = await db.Set<AuditIntakeRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct);

        if (row is null)
            return null;

        return new AuditIntakeDetailView(
            row.Name,
            row.DisbursementVoucherName,
            row.ReceivedTimestamp,
            row.RecordedTimestamp,
            row.AuditResult,
            row.Findings,
            row.ReleasedTimestamp,
            row.ReleasedTo,
            row.Status);
    }

    public async Task<AuditIntakeView> AddAsync(CreateAuditIntakeCommand cmd, CancellationToken ct)
    {
        var year = cmd.ReceivedTimestamp.Year;
        var series = $"AIA-{year}";

        var counter = await db.Set<VoucherCounter>()
            .Where(c => c.Series == series)
            .FirstOrDefaultAsync(ct);

        if (counter is null)
        {
            counter = new VoucherCounter { Series = series, Current = 0 };
            db.Add(counter);
        }

        counter.Current++;
        var name = $"{series}-{counter.Current:D5}";

        var row = new AuditIntakeRow
        {
            Name = name,
            DisbursementVoucherName = cmd.DisbursementVoucherName,
            ReceivedTimestamp = cmd.ReceivedTimestamp,
            RecordedTimestamp = null,
            AuditResult = "Pending",
            Findings = null,
            ReleasedTimestamp = null,
            ReleasedTo = null,
            Status = "Received",
        };

        db.Add(row);
        await db.SaveChangesAsync(ct);

        return new AuditIntakeView(
            row.Name,
            row.DisbursementVoucherName,
            row.Status,
            row.AuditResult);
    }

    public async Task UpdateAsync(
        string name,
        string status,
        string? auditResult,
        string? findings,
        DateTime? releasedTimestamp,
        string? releasedTo,
        CancellationToken ct)
    {
        var row = await db.Set<AuditIntakeRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"Audit Intake '{name}' not found.");

        row.Status = status;

        if (status == "Recorded")
            row.RecordedTimestamp = DateTime.UtcNow;

        if (auditResult is not null)
            row.AuditResult = auditResult;

        if (findings is not null)
            row.Findings = findings;

        if (releasedTimestamp.HasValue)
            row.ReleasedTimestamp = releasedTimestamp;

        if (releasedTo is not null)
            row.ReleasedTo = releasedTo;

        await db.SaveChangesAsync(ct);
    }
}
