namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>One line item of an RCD, referencing an Official Receipt.</summary>
public sealed class RcdLineRow
{
    public int Id { get; set; }
    public string ParentRcdName { get; set; } = default!;
    public string OfficialReceiptName { get; set; } = default!;
    public string? OrNumber { get; set; }
    public DateOnly? PostingDate { get; set; }
    public string? Payor { get; set; }
    public string? ModeOfPayment { get; set; }
    public decimal AmountCollected { get; set; }

    public ReportOfCollectionsRow Parent { get; set; } = default!;
}
