namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>JO/COS Payroll Entry header as stored. Mutable (transitions through Draft/HrTransmittalReceived/HoursValidated/Computed/Approved/Posted/Cancelled).</summary>
public sealed class JoCosPayrollEntryRow
{
    public string Name { get; set; } = default!;
    public string EmployeeType { get; set; } = default!;
    public string PayrollPeriod { get; set; } = default!;
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public DateOnly PostingDate { get; set; }
    public string? FundCluster { get; set; }
    public string? HrTransmittalReference { get; set; }
    public DateOnly? HrTransmittalReceivedDate { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalDays { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalNet { get; set; }
    public bool HoursValidated { get; set; }
    public bool DtrValidated { get; set; }
    public bool AccomplishmentValidated { get; set; }
    public string? ValidationRemarks { get; set; }
    public string Status { get; set; } = default!;
    public string? GlPostingReference { get; set; }
    public string? Remarks { get; set; }

    public List<JoCosPayrollLineRow> Lines { get; set; } = [];
}
