using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Application.ReferenceData;
using CvSU.Ais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CvSU.Ais.Infrastructure.Repositories;

// ── PAP Code ──────────────────────────────────────────────────────────────────

public sealed class PapCodeRepository(AisDbContext db) : IPapCodeRepository
{
    public Task<IReadOnlyList<PapCodeView>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Set<PapCodeRow>()
          .OrderBy(x => x.Code)
          .Select(x => new PapCodeView(x.Code, x.Description, x.ParentCode, x.IsGroup))
          .ToListAsync(cancellationToken)
          .ContinueWith(t => (IReadOnlyList<PapCodeView>)t.Result, TaskContinuationOptions.ExecuteSynchronously);

    public Task<PapCodeView?> GetAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<PapCodeRow>()
          .Where(x => x.Code == code)
          .Select(x => new PapCodeView(x.Code, x.Description, x.ParentCode, x.IsGroup))
          .FirstOrDefaultAsync(cancellationToken);

    public async Task<PapCodeView> AddAsync(CreatePapCodeCommand command, CancellationToken cancellationToken = default)
    {
        var exists = await db.Set<PapCodeRow>()
            .AnyAsync(x => x.Code == command.Code, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"PAP code '{command.Code}' already exists.");

        var row = new PapCodeRow
        {
            Code        = command.Code,
            Description = command.Description,
            ParentCode  = command.ParentCode,
            IsGroup     = command.IsGroup,
        };
        db.Set<PapCodeRow>().Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new PapCodeView(row.Code, row.Description, row.ParentCode, row.IsGroup);
    }
}

// ── Location Code ─────────────────────────────────────────────────────────────

public sealed class LocationCodeRepository(AisDbContext db) : ILocationCodeRepository
{
    public Task<IReadOnlyList<LocationCodeView>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Set<LocationCodeRow>()
          .OrderBy(x => x.PsgcCode)
          .Select(x => new LocationCodeView(x.PsgcCode, x.LocationName, x.Level, x.ParentCode, x.IsGroup))
          .ToListAsync(cancellationToken)
          .ContinueWith(t => (IReadOnlyList<LocationCodeView>)t.Result, TaskContinuationOptions.ExecuteSynchronously);

    public Task<LocationCodeView?> GetAsync(string psgcCode, CancellationToken cancellationToken = default) =>
        db.Set<LocationCodeRow>()
          .Where(x => x.PsgcCode == psgcCode)
          .Select(x => new LocationCodeView(x.PsgcCode, x.LocationName, x.Level, x.ParentCode, x.IsGroup))
          .FirstOrDefaultAsync(cancellationToken);

    public async Task<LocationCodeView> AddAsync(CreateLocationCodeCommand command, CancellationToken cancellationToken = default)
    {
        var exists = await db.Set<LocationCodeRow>()
            .AnyAsync(x => x.PsgcCode == command.PsgcCode, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Location code '{command.PsgcCode}' already exists.");

        var row = new LocationCodeRow
        {
            PsgcCode     = command.PsgcCode,
            LocationName = command.LocationName,
            Level        = command.Level,
            ParentCode   = command.ParentCode,
            IsGroup      = command.IsGroup,
        };
        db.Set<LocationCodeRow>().Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new LocationCodeView(row.PsgcCode, row.LocationName, row.Level, row.ParentCode, row.IsGroup);
    }
}

// ── Operational Fund ──────────────────────────────────────────────────────────

public sealed class OperationalFundRepository(AisDbContext db) : IOperationalFundRepository
{
    public Task<IReadOnlyList<OperationalFundView>> ListAsync(CancellationToken cancellationToken = default) =>
        db.Set<OperationalFundRow>()
          .OrderBy(x => x.Code)
          .Select(x => new OperationalFundView(x.Code, x.FundName, x.FundType, x.ParentClusterCode, x.IsActive))
          .ToListAsync(cancellationToken)
          .ContinueWith(t => (IReadOnlyList<OperationalFundView>)t.Result, TaskContinuationOptions.ExecuteSynchronously);

    public Task<OperationalFundView?> GetAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<OperationalFundRow>()
          .Where(x => x.Code == code)
          .Select(x => new OperationalFundView(x.Code, x.FundName, x.FundType, x.ParentClusterCode, x.IsActive))
          .FirstOrDefaultAsync(cancellationToken);

    public async Task<OperationalFundView> AddAsync(CreateOperationalFundCommand command, CancellationToken cancellationToken = default)
    {
        var exists = await db.Set<OperationalFundRow>()
            .AnyAsync(x => x.Code == command.Code, cancellationToken);
        if (exists)
            throw new InvalidOperationException($"Operational fund '{command.Code}' already exists.");

        var row = new OperationalFundRow
        {
            Code              = command.Code,
            FundName          = command.FundName,
            FundType          = command.FundType,
            ParentClusterCode = command.ParentClusterCode,
            IsActive          = true,
        };
        db.Set<OperationalFundRow>().Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new OperationalFundView(row.Code, row.FundName, row.FundType, row.ParentClusterCode, row.IsActive);
    }
}
