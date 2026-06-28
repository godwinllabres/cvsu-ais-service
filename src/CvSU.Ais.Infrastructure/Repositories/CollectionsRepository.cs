using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Collections;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ---------------------------------------------------------------------------
// Order of Payment
// ---------------------------------------------------------------------------

public sealed class OrderOfPaymentRepository(AisDbContext db) : IOrderOfPaymentRepository
{
    public async Task<IReadOnlyList<OrderOfPaymentView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<OrderOfPaymentRow>()
            .OrderBy(r => r.Name)
            .Select(r => new OrderOfPaymentView(r.Name, r.OrderDate, r.Customer, r.Amount, r.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderOfPaymentDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<OrderOfPaymentRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task<OrderOfPaymentDetailView> AddAsync(
        CreateOrderOfPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await db.Set<OrderOfPaymentRow>()
            .CountAsync(r => r.OrderDate == today, cancellationToken);
        var name = $"OP-{today:yyyy-MM-dd}-{count + 1:D4}";

        var row = new OrderOfPaymentRow
        {
            Name = name,
            OrderDate = command.OrderDate,
            Customer = command.Customer,
            Description = command.Description,
            Amount = command.Amount,
            FundCluster = command.FundCluster,
            Status = "Draft",
            IssuedBy = command.IssuedBy,
            Remarks = command.Remarks,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    public async Task<OrderOfPaymentDetailView> UpdateStatusAsync(
        string name,
        string status,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<OrderOfPaymentRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Order of Payment '{name}' not found.");
        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    private static OrderOfPaymentDetailView ToDetail(OrderOfPaymentRow r) => new(
        r.Name, r.OrderDate, r.Customer, r.Description, r.Amount,
        r.FundCluster, r.Status, r.IssuedBy, r.Remarks);
}

// ---------------------------------------------------------------------------
// Official Receipt
// ---------------------------------------------------------------------------

public sealed class OfficialReceiptRepository(AisDbContext db) : IOfficialReceiptRepository
{
    public async Task<IReadOnlyList<OfficialReceiptView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<OfficialReceiptRow>()
            .OrderBy(r => r.Name)
            .Select(r => new OfficialReceiptView(
                r.Name, r.OrNumber, r.PostingDate, r.Customer,
                r.AmountPaid, r.ModeOfPayment, r.CollectionStatus))
            .ToListAsync(cancellationToken);
    }

    public async Task<OfficialReceiptDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<OfficialReceiptRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task<OfficialReceiptDetailView> AddAsync(
        CreateOfficialReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await db.Set<OfficialReceiptRow>()
            .CountAsync(r => r.PostingDate == today, cancellationToken);
        var name = $"OR-{today:yyyy-MM-dd}-{count + 1:D4}";

        var row = new OfficialReceiptRow
        {
            Name = name,
            OrNumber = command.OrNumber,
            PostingDate = command.PostingDate,
            OrderOfPaymentName = command.OrderOfPaymentName,
            Customer = command.Customer,
            AmountPaid = command.AmountPaid,
            ModeOfPayment = command.ModeOfPayment,
            FundCluster = command.FundCluster,
            CollectionStatus = "Draft",
            Remarks = command.Remarks,
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    public async Task<OfficialReceiptDetailView> UpdateStatusAsync(
        string name,
        string status,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<OfficialReceiptRow>()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Official Receipt '{name}' not found.");
        row.CollectionStatus = status;
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    private static OfficialReceiptDetailView ToDetail(OfficialReceiptRow r) => new(
        r.Name, r.OrNumber, r.PostingDate, r.OrderOfPaymentName,
        r.Customer, r.AmountPaid, r.ModeOfPayment, r.FundCluster,
        r.CollectionStatus, r.Remarks);
}

// ---------------------------------------------------------------------------
// RCD (Report of Collections and Deposits)
// ---------------------------------------------------------------------------

public sealed class RcdRepository(AisDbContext db) : IRcdRepository
{
    public async Task<IReadOnlyList<RcdView>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.Set<ReportOfCollectionsRow>()
            .OrderBy(r => r.Name)
            .Select(r => new RcdView(r.Name, r.ReportDate, r.CollectingOfficer, r.TotalCollected, r.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<RcdDetailView?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var row = await db.Set<ReportOfCollectionsRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task<RcdDetailView> AddAsync(
        CreateRcdCommand command,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await db.Set<ReportOfCollectionsRow>()
            .CountAsync(r => r.ReportDate == today, cancellationToken);
        var name = $"RCD-{today:yyyy-MM-dd}-{count + 1:D4}";

        var totalCollected = command.Lines.Sum(l => l.AmountCollected);

        var row = new ReportOfCollectionsRow
        {
            Name = name,
            ReportDate = command.ReportDate,
            FiscalYear = command.FiscalYear,
            FundCluster = command.FundCluster,
            CollectingOfficer = command.CollectingOfficer,
            DepositSlipNo = command.DepositSlipNo,
            DepositDate = command.DepositDate,
            DepositoryBank = command.DepositoryBank,
            DepositAccountNumber = command.DepositAccountNumber,
            TotalCollected = totalCollected,
            TotalDeposited = command.TotalDeposited,
            Status = "Draft",
            Remarks = command.Remarks,
            Lines = command.Lines.Select(l => new RcdLineRow
            {
                OfficialReceiptName = l.OfficialReceiptName,
                OrNumber = l.OrNumber,
                PostingDate = l.PostingDate,
                Payor = l.Payor,
                ModeOfPayment = l.ModeOfPayment,
                AmountCollected = l.AmountCollected,
            }).ToList(),
        };

        db.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    public async Task<RcdDetailView> UpdateStatusAsync(
        string name,
        string status,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Set<ReportOfCollectionsRow>()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Report of Collections and Deposits '{name}' not found.");
        row.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(row);
    }

    private static RcdDetailView ToDetail(ReportOfCollectionsRow r) => new(
        r.Name, r.ReportDate, r.FiscalYear, r.FundCluster,
        r.CollectingOfficer, r.DepositSlipNo, r.DepositDate,
        r.DepositoryBank, r.DepositAccountNumber,
        r.TotalCollected, r.TotalDeposited, r.Status, r.Remarks,
        r.Lines.Select(l => new RcdLineDto(
            l.OfficialReceiptName, l.OrNumber, l.PostingDate,
            l.Payor, l.ModeOfPayment, l.AmountCollected))
        .ToList());
}
