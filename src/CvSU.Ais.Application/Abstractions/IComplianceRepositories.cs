using CvSU.Ais.Application.Compliance;

namespace CvSU.Ais.Application.Abstractions;

public interface ICoaCaseRepository
{
    Task<IReadOnlyList<CoaCaseView>> ListAsync(CancellationToken cancellationToken = default);
    Task<CoaCaseDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(CoaCaseDetailView detail, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, CancellationToken cancellationToken = default);
}

public interface IBir2307Repository
{
    Task<IReadOnlyList<Bir2307View>> ListAsync(CancellationToken cancellationToken = default);
    Task<Bir2307DetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Bir2307DetailView detail, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, string? reviewedBy, DateTime? reviewedOn, CancellationToken cancellationToken = default);
}

public interface IWhtStatementRepository
{
    Task<IReadOnlyList<WhtStatementView>> ListAsync(CancellationToken cancellationToken = default);
    Task<WhtStatementDetailView?> GetAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(WhtStatementDetailView detail, IReadOnlyList<WhtLineDto> lines, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string name, string status, string? reviewedBy, DateTime? reviewedOn, CancellationToken cancellationToken = default);

    /// <summary>Stamps the GL posting reference after Remittance statement is posted.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken cancellationToken = default);
}

public interface IStateHistoryRepository
{
    Task<IReadOnlyList<StateHistoryView>> ListForDocumentAsync(string doctype, string name, CancellationToken ct);
    Task RecordAsync(StateHistoryView entry, CancellationToken ct);
}
