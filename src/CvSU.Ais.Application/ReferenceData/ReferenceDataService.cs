using CvSU.Ais.Application.Abstractions;

namespace CvSU.Ais.Application.ReferenceData;

// ── PAP Code ──────────────────────────────────────────────────────────────────

public sealed record PapCodeView(
    string Code,
    string Description,
    string? ParentCode,
    bool IsGroup);

public sealed record CreatePapCodeCommand(
    string Code,
    string Description,
    string? ParentCode,
    bool IsGroup);

public sealed class PapCodeService(IPapCodeRepository repo)
{
    public Task<IReadOnlyList<PapCodeView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public Task<PapCodeView?> GetAsync(string code, CancellationToken cancellationToken = default) =>
        repo.GetAsync(code, cancellationToken);

    public Task<PapCodeView> AddAsync(CreatePapCodeCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);
}

// ── Location Code ─────────────────────────────────────────────────────────────

public sealed record LocationCodeView(
    string PsgcCode,
    string LocationName,
    string Level,
    string? ParentCode,
    bool IsGroup);

public sealed record CreateLocationCodeCommand(
    string PsgcCode,
    string LocationName,
    string Level,
    string? ParentCode,
    bool IsGroup);

public sealed class LocationCodeService(ILocationCodeRepository repo)
{
    public Task<IReadOnlyList<LocationCodeView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public Task<LocationCodeView?> GetAsync(string psgcCode, CancellationToken cancellationToken = default) =>
        repo.GetAsync(psgcCode, cancellationToken);

    public Task<LocationCodeView> AddAsync(CreateLocationCodeCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);
}

// ── Operational Fund ──────────────────────────────────────────────────────────

public sealed record OperationalFundView(
    string Code,
    string FundName,
    string FundType,
    string? ParentClusterCode,
    bool IsActive);

public sealed record CreateOperationalFundCommand(
    string Code,
    string FundName,
    string FundType,
    string? ParentClusterCode);

public sealed class OperationalFundService(IOperationalFundRepository repo)
{
    public Task<IReadOnlyList<OperationalFundView>> ListAsync(CancellationToken cancellationToken = default) =>
        repo.ListAsync(cancellationToken);

    public Task<OperationalFundView?> GetAsync(string code, CancellationToken cancellationToken = default) =>
        repo.GetAsync(code, cancellationToken);

    public Task<OperationalFundView> AddAsync(CreateOperationalFundCommand command, CancellationToken cancellationToken = default) =>
        repo.AddAsync(command, cancellationToken);
}
