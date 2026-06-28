namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>A single line item on an ORS/BURS. Owned by
/// <see cref="ObligationRequestRow"/> via the ParentOrsName foreign key.</summary>
public sealed class OrsLineItemRow
{
    public int Id { get; set; }
    public string ParentOrsName { get; set; } = default!;
    public string Particulars { get; set; } = default!;
    public string? AllotmentId { get; set; }
    public decimal Amount { get; set; }
    public string? PapCode { get; set; }
    public string? LocationCode { get; set; }
    public string? ExpenseClass { get; set; }
    public string? Remarks { get; set; }

    public ObligationRequestRow Parent { get; set; } = default!;
}
