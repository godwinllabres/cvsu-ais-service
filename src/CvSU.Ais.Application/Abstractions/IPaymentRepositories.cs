using CvSU.Ais.Application.Payments;

namespace CvSU.Ais.Application.Abstractions;

public interface ILddapAdaRepository
{
    Task<IReadOnlyList<LddapAdaView>> ListAsync(CancellationToken ct);
    Task<LddapAdaDetailView?> GetAsync(string name, CancellationToken ct);
    Task<LddapAdaView> AddAsync(CreateLddapAdaCommand cmd, CancellationToken ct);
    Task UpdateStatusAsync(string name, string status, DateOnly? transmittedDate, CancellationToken ct);
}

public interface IDvTransmittalRepository
{
    Task<IReadOnlyList<DvTransmittalView>> ListAsync(CancellationToken ct);
    Task<DvTransmittalDetailView?> GetAsync(string name, CancellationToken ct);
    Task<DvTransmittalView> AddAsync(CreateDvTransmittalCommand cmd, CancellationToken ct);
    Task UpdateStatusAsync(string name, string status, string? receivedBy, DateOnly? receivedDate, CancellationToken ct);
}

public interface IAuditIntakeRepository
{
    Task<IReadOnlyList<AuditIntakeView>> ListAsync(CancellationToken ct);
    Task<AuditIntakeDetailView?> GetAsync(string name, CancellationToken ct);
    Task<AuditIntakeView> AddAsync(CreateAuditIntakeCommand cmd, CancellationToken ct);
    Task UpdateAsync(string name, string status, string? auditResult, string? findings, DateTime? releasedTimestamp, string? releasedTo, CancellationToken ct);
}
