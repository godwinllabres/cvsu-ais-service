using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.Obligations;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

public sealed class ObligationRequestRepository(
    AisDbContext db,
    IVoucherNumberGenerator numbers,
    IUnitOfWork unitOfWork) : IObligationRequestRepository
{
    public async Task<IReadOnlyList<OrsView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<ObligationRequestRow>()
            .OrderBy(r => r.Name)
            .Select(r => new OrsView(r.Name, r.PostingDate, r.RequestingUnit, r.Amount, r.Status))
            .ToListAsync(ct);
    }

    public async Task<OrsDetailView?> GetAsync(string name, CancellationToken ct)
    {
        var row = await db.Set<ObligationRequestRow>()
            .Include(r => r.LineItems)
            .FirstOrDefaultAsync(r => r.Name == name, ct);

        if (row is null)
            return null;

        var lineItems = row.LineItems
            .Select(li => new OrsLineItemDto(
                li.Particulars,
                li.AllotmentId,
                li.Amount,
                li.PapCode,
                li.LocationCode,
                li.ExpenseClass,
                li.Remarks))
            .ToList();

        return new OrsDetailView(
            row.Name,
            row.PostingDate,
            row.FiscalYear,
            row.RequestingUnit,
            row.Purpose,
            row.Amount,
            row.FundingSourceCode,
            row.PapCode,
            row.LocationCode,
            row.ExpenseClass,
            row.Status,
            row.RequestingOfficeUser,
            row.BudgetOfficerUser,
            row.Remarks,
            lineItems);
    }

    public Task<OrsView> AddAsync(CreateOrsCommand command, CancellationToken ct) =>
        // The gapless generator serializes issuers of this series via a transaction-scoped
        // advisory lock, and the counter increment must commit or roll back together with the
        // ORS row — so the create runs inside a single ambient transaction.
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var series = $"ORSB-{command.FiscalYear}";
            var name = await numbers.NextAsync(series, token);

            var row = new ObligationRequestRow
            {
                Name = name,
                PostingDate = command.PostingDate,
                FiscalYear = command.FiscalYear,
                RequestingUnit = command.RequestingUnit,
                Purpose = command.Purpose,
                Amount = command.Amount,
                FundingSourceCode = command.FundingSourceCode,
                PapCode = command.PapCode,
                LocationCode = command.LocationCode,
                ExpenseClass = command.ExpenseClass,
                Status = "Draft",
                RequestingOfficeUser = command.RequestingOfficeUser,
                BudgetOfficerUser = command.BudgetOfficerUser,
                Remarks = command.Remarks,
                LineItems = command.LineItems.Select(li => new OrsLineItemRow
                {
                    Particulars = li.Particulars,
                    AllotmentId = li.AllotmentId,
                    Amount = li.Amount,
                    PapCode = li.PapCode,
                    LocationCode = li.LocationCode,
                    ExpenseClass = li.ExpenseClass,
                    Remarks = li.Remarks,
                }).ToList(),
            };

            db.Add(row);
            await db.SaveChangesAsync(token);

            return new OrsView(row.Name, row.PostingDate, row.RequestingUnit, row.Amount, row.Status);
        }, ct);

    public async Task UpdateStatusAsync(string name, string newStatus, CancellationToken ct)
    {
        var row = await db.Set<ObligationRequestRow>()
            .FirstOrDefaultAsync(r => r.Name == name, ct)
            ?? throw new KeyNotFoundException($"ORS/BURS '{name}' not found.");

        row.Status = newStatus;
        await db.SaveChangesAsync(ct);
    }
}

public sealed class NcaRepository(AisDbContext db) : INcaRepository
{
    public async Task<IReadOnlyList<NcaView>> ListAsync(CancellationToken ct)
    {
        return await db.Set<NoticeOfCashAllocationRow>()
            .OrderBy(r => r.NcaNumber)
            .Select(r => new NcaView(
                r.NcaNumber,
                r.DateReceived,
                r.FiscalYear,
                r.FundingSourceCode,
                r.ValidityDate,
                r.Status,
                r.NcaAmount,
                r.UtilizedAmount))
            .ToListAsync(ct);
    }

    public async Task<NcaView?> GetAsync(string ncaNumber, CancellationToken ct)
    {
        var row = await db.Set<NoticeOfCashAllocationRow>()
            .FirstOrDefaultAsync(r => r.NcaNumber == ncaNumber, ct);

        if (row is null)
            return null;

        return new NcaView(
            row.NcaNumber,
            row.DateReceived,
            row.FiscalYear,
            row.FundingSourceCode,
            row.ValidityDate,
            row.Status,
            row.NcaAmount,
            row.UtilizedAmount);
    }

    public async Task<NcaView> AddAsync(CreateNcaCommand command, CancellationToken ct)
    {
        var row = new NoticeOfCashAllocationRow
        {
            NcaNumber = command.NcaNumber,
            DateReceived = command.DateReceived,
            FiscalYear = command.FiscalYear,
            FundingSourceCode = command.FundingSourceCode,
            ValidityDate = command.ValidityDate,
            Status = "Draft",
            NcaAmount = command.NcaAmount,
            UtilizedAmount = 0m,
            Remarks = command.Remarks,
        };

        db.Add(row);
        await db.SaveChangesAsync(ct);

        return new NcaView(
            row.NcaNumber,
            row.DateReceived,
            row.FiscalYear,
            row.FundingSourceCode,
            row.ValidityDate,
            row.Status,
            row.NcaAmount,
            row.UtilizedAmount);
    }
}
