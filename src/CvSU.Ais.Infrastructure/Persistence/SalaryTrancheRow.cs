namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Salary Tranche header as stored. Mutable (transitions through Draft/Active/Superseded).</summary>
public sealed class SalaryTrancheRow
{
    public string Name { get; set; } = default!;
    public string SslLaw { get; set; } = default!;
    public int TrancheNumber { get; set; }
    public int EffectiveYear { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public string? DbmCircularReference { get; set; }
    public bool IsActive { get; set; }
    public int TotalEntries { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public string Status { get; set; } = default!;
    public string ImportStatus { get; set; } = default!;
    public string? ValidationErrors { get; set; }
    public string? Remarks { get; set; }

    public List<SalaryTrancheEntryRow> Entries { get; set; } = [];
}
