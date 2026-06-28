namespace CvSU.Ais.Infrastructure.Persistence;

/// <summary>Routing template header — defines a named re-usable review path.</summary>
public sealed class RoutingTemplateRow
{
    public string Name { get; set; } = default!;
    public string TemplateName { get; set; } = default!;
    public string? DocumentType { get; set; }
    public string? Description { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    public List<RoutingTemplateStepRow> Steps { get; set; } = [];
}
