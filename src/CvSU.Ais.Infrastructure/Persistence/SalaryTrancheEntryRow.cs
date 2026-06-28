namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Salary grade/step/amount entry belonging to a Salary Tranche.</summary>
public sealed class SalaryTrancheEntryRow
{
    public int Id { get; set; }
    public string ParentTrancheName { get; set; } = default!;
    public int SalaryGrade { get; set; }
    public int Step { get; set; }
    public decimal MonthlySalary { get; set; }

    public SalaryTrancheRow Parent { get; set; } = default!;
}
