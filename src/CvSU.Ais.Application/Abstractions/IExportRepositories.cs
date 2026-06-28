using CvSU.Ais.Application.Exports;

namespace CvSU.Ais.Application.Abstractions;

public interface IFindesExportRepository
{
    Task<IReadOnlyList<FindesExportView>> ListAsync(CancellationToken cancellationToken = default);

    Task<FindesExportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(FindesExportDetailView detail, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string name,
        string status,
        string? reviewedBy,
        DateTime? reviewedOn,
        string? generatedBy,
        DateTime? generatedOn,
        CancellationToken cancellationToken = default);
}

public interface IBankCollectionReportRepository
{
    Task<IReadOnlyList<BankCollectionReportView>> ListAsync(CancellationToken cancellationToken = default);

    Task<BankCollectionReportDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(BankCollectionReportDetailView detail, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);
}

public interface IPushTokenRepository
{
    Task<IReadOnlyList<PushTokenView>> ListForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task AddAsync(RegisterPushTokenCommand command, CancellationToken cancellationToken = default);

    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
