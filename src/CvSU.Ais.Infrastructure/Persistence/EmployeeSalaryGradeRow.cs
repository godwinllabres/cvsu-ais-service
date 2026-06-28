namespace CvSU.Ais.Infrastructure.Persistence;

public sealed class EmployeeSalaryGradeRow
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public int SalaryGrade { get; set; }
    public int Step { get; set; }
    public decimal MonthlySalary { get; set; }
    public DateOnly EffectiveDate { get; set; }
}
