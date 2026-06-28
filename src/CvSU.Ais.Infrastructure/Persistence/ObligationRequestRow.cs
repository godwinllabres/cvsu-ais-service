namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>ORS/BURS header row. Status is persisted as a readable string.
/// Line items are stored separately in <see cref="OrsLineItemRow"/>.</summary>
public sealed class ObligationRequestRow
{
    public string Name { get; set; } = default!;
    public DateOnly PostingDate { get; set; }
    public int FiscalYear { get; set; }
    public string RequestingUnit { get; set; } = default!;
    public string Purpose { get; set; } = default!;
    public decimal Amount { get; set; }
    public string FundingSourceCode { get; set; } = default!;
    public string? PapCode { get; set; }
    public string? LocationCode { get; set; }
    public string? ExpenseClass { get; set; }
    public string Status { get; set; } = default!;
    public string? RequestingOfficeUser { get; set; }
    public string? BudgetOfficerUser { get; set; }
    public string? Remarks { get; set; }

    public ICollection<OrsLineItemRow> LineItems { get; set; } = new List<OrsLineItemRow>();
}
