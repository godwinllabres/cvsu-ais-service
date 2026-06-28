using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Compliance;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ── COA Case ──────────────────────────────────────────────────────────────────

public sealed class CoaCaseRepository(AisDbContext db) : ICoaCaseRepository
{
    public async Task<IReadOnlyList<CoaCaseView>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<CoaCaseRow>()
            .OrderBy(r => r.Name)
            .Select(r => new CoaCaseView(r.Name, r.NdNcReference, r.LiableParty, r.Amount, r.Status))
            .ToListAsync(cancellationToken);

    public async Task<CoaCaseDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<CoaCaseRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task AddAsync(CoaCaseDetailView detail, CancellationToken cancellationToken = default)
    {
        db.Add(ToRow(detail));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<CoaCaseRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"COA Case '{name}' not found for status update.");
        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static CoaCaseRow ToRow(CoaCaseDetailView d) => new()
    {
        Name = d.Name,
        NdNcReference = d.NdNcReference,
        NfdReference = d.NfdReference,
        CoeReference = d.CoeReference,
        LiableParty = d.LiableParty,
        Amount = d.Amount,
        SettlementMode = d.SettlementMode,
        OrReference = d.OrReference,
        Status = d.Status,
        Remarks = d.Remarks,
    };

    private static CoaCaseDetailView ToDetail(CoaCaseRow r) => new(
        r.Name, r.NdNcReference, r.NfdReference, r.CoeReference,
        r.LiableParty, r.Amount, r.SettlementMode, r.OrReference,
        r.Status, r.Remarks);
}

// ── BIR 2307 ─────────────────────────────────────────────────────────────────

public sealed class Bir2307Repository(AisDbContext db) : IBir2307Repository
{
    public async Task<IReadOnlyList<Bir2307View>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<Bir2307Row>()
            .OrderBy(r => r.Name)
            .Select(r => new Bir2307View(
                r.Name, r.DvReference, r.PayeeName, r.GrossAmount, r.EwtAmount, r.ApprovalStatus))
            .ToListAsync(cancellationToken);

    public async Task<Bir2307DetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<Bir2307Row>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task AddAsync(Bir2307DetailView detail, CancellationToken cancellationToken = default)
    {
        db.Add(ToRow(detail));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        string name, string status, string? reviewedBy, DateTime? reviewedOn,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<Bir2307Row>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"BIR 2307 '{name}' not found for status update.");
        row.ApprovalStatus = status;
        row.ReviewedBy = reviewedBy;
        row.ReviewedOn = reviewedOn;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Bir2307Row ToRow(Bir2307DetailView d) => new()
    {
        Name = d.Name,
        DvReference = d.DvReference,
        PeriodFrom = d.PeriodFrom,
        PeriodTo = d.PeriodTo,
        PayeeName = d.PayeeName,
        PayeeTin = d.PayeeTin,
        PayeeAddress = d.PayeeAddress,
        IncomePaymentType = d.IncomePaymentType,
        GrossAmount = d.GrossAmount,
        EwtRate = d.EwtRate,
        EwtAmount = d.EwtAmount,
        NetAmount = d.NetAmount,
        ApprovalStatus = d.ApprovalStatus,
        ReviewedBy = d.ReviewedBy,
        ReviewedOn = d.ReviewedOn,
        Remarks = d.Remarks,
    };

    private static Bir2307DetailView ToDetail(Bir2307Row r) => new(
        r.Name, r.DvReference, r.PeriodFrom, r.PeriodTo,
        r.PayeeName, r.PayeeTin, r.PayeeAddress, r.IncomePaymentType,
        r.GrossAmount, r.EwtRate, r.EwtAmount, r.NetAmount,
        r.ApprovalStatus, r.ReviewedBy, r.ReviewedOn, r.Remarks);
}

// ── Withholding Tax Statement ─────────────────────────────────────────────────

public sealed class WhtStatementRepository(AisDbContext db) : IWhtStatementRepository
{
    public async Task<IReadOnlyList<WhtStatementView>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<WithholdingTaxStatementRow>()
            .OrderBy(r => r.Name)
            .Select(r => new WhtStatementView(
                r.Name, r.StatementType, r.PostingDate,
                r.GrossAmount, r.TotalTaxAmount, r.ApprovalStatus))
            .ToListAsync(cancellationToken);

    public async Task<WhtStatementDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var header = await db.Set<WithholdingTaxStatementRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (header is null)
            return null;

        var lines = await db.Set<WithholdingTaxLineRow>()
            .Where(l => l.ParentWhtName == name)
            .OrderBy(l => l.Id)
            .Select(l => new WhtLineDto(
                l.TaxType, l.TaxClass, l.AtcCode,
                l.Rate, l.TaxBase, l.TaxAmount,
                l.LiabilityAccount, l.SourceDv, l.Remarks))
            .ToListAsync(cancellationToken);

        return ToDetail(header, lines);
    }

    public async Task AddAsync(
        WhtStatementDetailView detail,
        IReadOnlyList<WhtLineDto> lines,
        CancellationToken cancellationToken = default)
    {
        db.Add(ToHeaderRow(detail));
        foreach (var line in lines)
            db.Add(ToLineRow(detail.Name, line));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        string name, string status, string? reviewedBy, DateTime? reviewedOn,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<WithholdingTaxStatementRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"WHT Statement '{name}' not found for status update.");
        row.ApprovalStatus = status;
        row.ReviewedBy = reviewedBy;
        row.ReviewedOn = reviewedOn;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetGlReferenceAsync(
        string name, string glRef, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<WithholdingTaxStatementRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"WHT Statement '{name}' not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static WithholdingTaxStatementRow ToHeaderRow(WhtStatementDetailView d) => new()
    {
        Name = d.Name,
        StatementType = d.StatementType,
        PostingDate = d.PostingDate,
        TaxPeriodMonth = d.TaxPeriodMonth,
        FundCluster = d.FundCluster,
        FundingSourceCode = d.FundingSourceCode,
        PayeeName = d.PayeeName,
        PayeeTin = d.PayeeTin,
        GrossAmount = d.GrossAmount,
        TotalTaxAmount = d.TotalTaxAmount,
        NetAmount = d.NetAmount,
        ApprovalStatus = d.ApprovalStatus,
        ReviewedBy = d.ReviewedBy,
        ReviewedOn = d.ReviewedOn,
        GlPostingReference = d.GlPostingReference,
        Remarks = d.Remarks,
    };

    private static WithholdingTaxLineRow ToLineRow(string parentName, WhtLineDto l) => new()
    {
        ParentWhtName = parentName,
        TaxType = l.TaxType,
        TaxClass = l.TaxClass,
        AtcCode = l.AtcCode,
        Rate = l.Rate,
        TaxBase = l.TaxBase,
        TaxAmount = l.TaxAmount,
        LiabilityAccount = l.LiabilityAccount,
        SourceDv = l.SourceDv,
        Remarks = l.Remarks,
    };

    private static WhtStatementDetailView ToDetail(WithholdingTaxStatementRow h, IReadOnlyList<WhtLineDto> lines) =>
        new(h.Name, h.StatementType, h.PostingDate, h.TaxPeriodMonth,
            h.FundCluster, h.FundingSourceCode, h.PayeeName, h.PayeeTin,
            h.GrossAmount, h.TotalTaxAmount, h.NetAmount,
            h.ApprovalStatus, h.ReviewedBy, h.ReviewedOn,
            h.GlPostingReference, h.Remarks, lines);
}

// ── State History ─────────────────────────────────────────────────────────────

public sealed class StateHistoryRepository(AisDbContext db) : IStateHistoryRepository
{
    public async Task<IReadOnlyList<StateHistoryView>> ListForDocumentAsync(
        string doctype, string name, CancellationToken ct) =>
        await db.Set<StateHistoryRow>()
            .Where(r => r.ReferenceDoctype == doctype && r.ReferenceName == name)
            .OrderBy(r => r.Timestamp)
            .Select(r => new StateHistoryView(
                r.Id, r.ReferenceDoctype, r.ReferenceName,
                r.FromState, r.ToState, r.Action,
                r.ActingUser, r.Timestamp, r.Remarks))
            .ToListAsync(ct);

    public async Task RecordAsync(StateHistoryView entry, CancellationToken ct)
    {
        db.Add(new StateHistoryRow
        {
            ReferenceDoctype = entry.ReferenceDoctype,
            ReferenceName = entry.ReferenceName,
            FromState = entry.FromState,
            ToState = entry.ToState,
            Action = entry.Action,
            ActingUser = entry.ActingUser,
            Timestamp = entry.Timestamp,
            Remarks = entry.Remarks,
        });
        await db.SaveChangesAsync(ct);
    }
}
