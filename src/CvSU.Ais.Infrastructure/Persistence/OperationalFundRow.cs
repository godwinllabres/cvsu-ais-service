namespace CvSU.Ais.Infrastructure.Persistence;

public sealed class OperationalFundRow
{
    public string Code { get; set; } = default!;
    public string FundName { get; set; } = default!;
    /// <summary>"General Fund", "Income Fund", "Trust Fund", "Performance Bond", "Other"</summary>
    public string FundType { get; set; } = default!;
    /// <summary>FK to funding_source.code — the parent cluster this fund belongs to.</summary>
    public string? ParentClusterCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
