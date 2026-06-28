using CvSU.Ais.Application.JournalEntries;

namespace CvSU.Ais.Application.Abstractions;

public interface IJournalEntryRepository
{
    Task<IReadOnlyList<JournalEntryView>> ListAsync(CancellationToken ct);
    Task<JournalEntryDetailView?> GetAsync(string name, CancellationToken ct);
    Task<JournalEntryView> AddAsync(CreateJournalEntryCommand command, CancellationToken ct);
    Task UpdateStatusAsync(string name, string newStatus, string? approvedBy, CancellationToken ct);

    /// <summary>Stamps the GL posting reference after the JE has been posted to the ledger.</summary>
    Task SetGlReferenceAsync(string name, string glRef, CancellationToken ct);
}
