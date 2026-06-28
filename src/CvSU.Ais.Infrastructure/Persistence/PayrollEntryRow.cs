namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Regular Payroll Entry header as stored. Mutable (transitions through Draft/Validated/Posted/Cancelled).</summary>
public sealed class PayrollEntryRow
{
    public string Name { get; set; } = default!;
    public string? Title { get; set; }
    public string PayrollType { get; set; } = default!;
    public string PayrollPeriod { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public string? FundCluster { get; set; }
    public string ImportStatus { get; set; } = default!;
    public int TotalRecords { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalTaxWithheld { get; set; }
    public decimal TotalGsis { get; set; }
    public decimal TotalPagibig { get; set; }
    public decimal TotalPhilhealth { get; set; }
    public decimal TotalOtherDeductions { get; set; }
    public string Status { get; set; } = default!;
    public string? GlPostingReference { get; set; }
    public string? ValidationErrors { get; set; }
    public string? Remarks { get; set; }

    public List<PayrollLoanDeductionRow> LoanDeductions { get; set; } = [];
}
