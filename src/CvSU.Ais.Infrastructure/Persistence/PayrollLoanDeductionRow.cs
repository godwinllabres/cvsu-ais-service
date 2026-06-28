namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Loan deduction line belonging to a Regular Payroll Entry.</summary>
public sealed class PayrollLoanDeductionRow
{
    public int Id { get; set; }
    public string ParentPayrollName { get; set; } = default!;
    public string LoanType { get; set; } = default!;
    public string? LoanReference { get; set; }
    public string EmployeeId { get; set; } = default!;
    public string? EmployeeName { get; set; }
    public decimal Amount { get; set; }

    public PayrollEntryRow Parent { get; set; } = default!;
}
