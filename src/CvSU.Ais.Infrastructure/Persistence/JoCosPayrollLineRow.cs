namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Individual employee line for a JO/COS Payroll Entry.</summary>
public sealed class JoCosPayrollLineRow
{
    public int Id { get; set; }
    public string ParentJoCosName { get; set; } = default!;
    public string EmployeeId { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public string EmploymentType { get; set; } = default!;
    public decimal AuthorizedHours { get; set; }
    public decimal ActualHours { get; set; }
    public decimal TardinessHours { get; set; }
    public decimal ComputedHours { get; set; }
    public decimal DailyRate { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public bool DiscrepancyFlag { get; set; }
    public string? Remarks { get; set; }

    public JoCosPayrollEntryRow Parent { get; set; } = default!;
}
