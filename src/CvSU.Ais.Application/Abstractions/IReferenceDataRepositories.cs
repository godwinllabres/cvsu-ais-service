using CvSU.Ais.Application.ReferenceData;

namespace CvSU.Ais.Application.Abstractions;

public interface IPapCodeRepository
{
    Task<IReadOnlyList<PapCodeView>> ListAsync(CancellationToken cancellationToken = default);
    Task<PapCodeView?> GetAsync(string code, CancellationToken cancellationToken = default);
    Task<PapCodeView> AddAsync(CreatePapCodeCommand command, CancellationToken cancellationToken = default);
}

public interface ILocationCodeRepository
{
    Task<IReadOnlyList<LocationCodeView>> ListAsync(CancellationToken cancellationToken = default);
    Task<LocationCodeView?> GetAsync(string psgcCode, CancellationToken cancellationToken = default);
    Task<LocationCodeView> AddAsync(CreateLocationCodeCommand command, CancellationToken cancellationToken = default);
}

public interface IOperationalFundRepository
{
    Task<IReadOnlyList<OperationalFundView>> ListAsync(CancellationToken cancellationToken = default);
    Task<OperationalFundView?> GetAsync(string code, CancellationToken cancellationToken = default);
    Task<OperationalFundView> AddAsync(CreateOperationalFundCommand command, CancellationToken cancellationToken = default);
}
