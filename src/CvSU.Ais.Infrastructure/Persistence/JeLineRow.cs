namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One line of a Journal Entry as stored.</summary>
public sealed class JeLineRow
{
    public int Id { get; set; }
    public string ParentJeName { get; set; } = default!;
    public string Account { get; set; } = default!;
    public string? AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }

    public JournalEntryRow Parent { get; set; } = default!;
}
