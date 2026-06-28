using CvSU.Ais.Application.Obligations;

namespace CvSU.Ais.Application.Abstractions;

public interface IObligationRequestRepository
{
    Task<IReadOnlyList<OrsView>> ListAsync(CancellationToken ct);
    Task<OrsDetailView?> GetAsync(string name, CancellationToken ct);
    Task<OrsView> AddAsync(CreateOrsCommand command, CancellationToken ct);
    Task UpdateStatusAsync(string name, string newStatus, CancellationToken ct);
}

public interface INcaRepository
{
    Task<IReadOnlyList<NcaView>> ListAsync(CancellationToken ct);
    Task<NcaView?> GetAsync(string ncaNumber, CancellationToken ct);
    Task<NcaView> AddAsync(CreateNcaCommand command, CancellationToken ct);
}
