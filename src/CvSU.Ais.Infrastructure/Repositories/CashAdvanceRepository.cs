using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.CashAdvances;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class CashAdvanceRepository(AisDbContext db)
    : ICashAdvanceRepository, ILiquidationReportRepository
{
    // ─── ICashAdvanceRepository ───────────────────────────────────────────

    public async Task<IReadOnlyList<CashAdvanceView>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.Set<CashAdvanceRow>()
            .OrderBy(r => r.Name)
            .Select(r => new CashAdvanceView(
                r.Name, r.Employee, r.EmployeeName,
                r.AdvanceAmount, r.LiquidatedAmount, r.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashAdvanceDetailView?> GetAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await db.Set<CashAdvanceRow>()
            .Where(r => r.Name == name)
            .Select(r => new CashAdvanceDetailView(
                r.Name, r.Employee, r.EmployeeName, r.PostingDate,
                r.FundCluster, r.Purpose, r.AdvanceAmount,
                r.LiquidatedAmount, r.UnliquidatedBalance,
                r.DueDate, r.Status, r.GlPostingReference, r.Remarks))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> AddAsync(
        CreateCashAdvanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var year = command.PostingDate.Year;
        var shortId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var name = $"CA-{year}-{shortId}";

        var row = new CashAdvanceRow
        {
            Name = name,
            Employee = command.Employee,
            EmployeeName = command.EmployeeName,
            PostingDate = command.PostingDate,
            FundCluster = command.FundCluster,
            Purpose = command.Purpose,
            AdvanceAmount = command.AdvanceAmount,
            LiquidatedAmount = 0m,
            UnliquidatedBalance = command.AdvanceAmount,
            DueDate = command.DueDate,
            Status = "Draft",
            GlPostingReference = null,
            Remarks = command.Remarks,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return name;
    }

    public async Task UpdateStatusAsync(
        string name,
        string status,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<CashAdvanceRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Cash advance '{name}' not found.");

        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetGlReferenceAsync(
        string name, string glRef, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<CashAdvanceRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Cash advance '{name}' not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ─── ILiquidationReportRepository ────────────────────────────────────

    async Task<IReadOnlyList<LiquidationReportView>> ILiquidationReportRepository.ListAsync(
        CancellationToken cancellationToken)
    {
        return await db.Set<LiquidationReportRow>()
            .OrderBy(r => r.Name)
            .Select(r => new LiquidationReportView(
                r.Name, r.CashAdvanceName, r.EmployeeName,
                r.TotalLiquidated, r.Status))
            .ToListAsync(cancellationToken);
    }

    async Task<LiquidationReportDetailView?> ILiquidationReportRepository.GetAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var row = await db.Set<LiquidationReportRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        if (row is null)
            return null;

        var lines = row.Lines
            .Select(l => new LiquidationLineDto(
                l.ExpenseType, l.Description, l.Amount,
                l.ReceiptReference, l.ReceiptDate, l.AccountCode))
            .ToList();

        return new LiquidationReportDetailView(
            row.Name, row.CashAdvanceName, row.Employee, row.EmployeeName,
            row.PostingDate, row.FundCluster, row.TotalLiquidated,
            row.AdvanceAmount, row.RefundDue, row.ReimbursementDue,
            row.Status, row.GlPostingReference, row.Remarks, lines);
    }

    async Task<string> ILiquidationReportRepository.AddAsync(
        CreateLiquidationReportCommand command,
        CancellationToken cancellationToken)
    {
        // Look up the linked cash advance to pull employee and advance amount.
        var ca = await db.Set<CashAdvanceRow>()
            .FirstOrDefaultAsync(r => r.Name == command.CashAdvanceName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Cash advance '{command.CashAdvanceName}' not found.");

        var year = command.PostingDate.Year;
        var shortId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var name = $"LR-{year}-{shortId}";

        var totalLiquidated = command.Lines.Sum(l => l.Amount);
        // refund_due > 0 when employee spent less than advance (they owe back the excess)
        // reimbursement_due > 0 when employee spent more than advance (agency owes more)
        var refundDue = totalLiquidated < ca.AdvanceAmount
            ? ca.AdvanceAmount - totalLiquidated
            : 0m;
        var reimbursementDue = totalLiquidated > ca.AdvanceAmount
            ? totalLiquidated - ca.AdvanceAmount
            : 0m;

        var row = new LiquidationReportRow
        {
            Name = name,
            CashAdvanceName = command.CashAdvanceName,
            Employee = ca.Employee,
            EmployeeName = ca.EmployeeName,
            PostingDate = command.PostingDate,
            FundCluster = command.FundCluster,
            TotalLiquidated = totalLiquidated,
            AdvanceAmount = ca.AdvanceAmount,
            RefundDue = refundDue,
            ReimbursementDue = reimbursementDue,
            Status = "Draft",
            GlPostingReference = null,
            Remarks = command.Remarks,
            Lines = command.Lines.Select(l => new LiquidationLineRow
            {
                ParentLrName = name,
                ExpenseType = l.ExpenseType,
                Description = l.Description,
                Amount = l.Amount,
                ReceiptReference = l.ReceiptReference,
                ReceiptDate = l.ReceiptDate,
                AccountCode = l.AccountCode,
            }).ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return name;
    }

    async Task ILiquidationReportRepository.UpdateStatusAsync(
        string name,
        string status,
        CancellationToken cancellationToken)
    {
        var row = await db.Set<LiquidationReportRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Liquidation report '{name}' not found.");

        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }

    async Task ILiquidationReportRepository.SetGlReferenceAsync(
        string name, string glRef, CancellationToken cancellationToken)
    {
        var row = await db.Set<LiquidationReportRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Liquidation report '{name}' not found.");
        row.GlPostingReference = glRef;
        await db.SaveChangesAsync(cancellationToken);
    }

    async Task ILiquidationReportRepository.UpdateCashAdvanceLiquidatedAsync(
        string cashAdvanceName, decimal totalLiquidated, CancellationToken cancellationToken)
    {
        var ca = await db.Set<CashAdvanceRow>()
            .FirstOrDefaultAsync(r => r.Name == cashAdvanceName, cancellationToken)
            ?? throw new KeyNotFoundException($"Cash advance '{cashAdvanceName}' not found.");

        ca.LiquidatedAmount = totalLiquidated;
        ca.UnliquidatedBalance = ca.AdvanceAmount - totalLiquidated;

        if (ca.UnliquidatedBalance <= 0m)
            ca.Status = "FullyLiquidated";

        await db.SaveChangesAsync(cancellationToken);
    }
}
