using CvSU.Ais.Domain.Common;

namespace CvSU.Ais.Domain.Funds;

/// <summary>
/// The complete UACS tuple every posted budget line must carry. Constructing
/// one is the single enforcement point for UACS completeness (R-BUD-06):
/// a line with any missing dimension cannot be represented, so a half-specified
/// posting is unrepresentable rather than merely rejected late.
/// </summary>
public sealed record UacsCode
{
    public FundingSource FundingSource { get; }

    /// <summary>UACS PAP (Program/Activity/Project) code.</summary>
    public string PapCode { get; }

    /// <summary>UACS PSGC location code.</summary>
    public string LocationCode { get; }

    public ExpenseClass ExpenseClass { get; }

    /// <summary>RCA object account — the 8-digit object-of-expenditure code.</summary>
    public string ObjectAccountCode { get; }

    public UacsCode(
        FundingSource fundingSource,
        string papCode,
        string locationCode,
        ExpenseClass expenseClass,
        string objectAccountCode)
    {
        if (fundingSource is null)
            throw new UacsIncompleteException("UACS is incomplete: funding source is required.");
        if (string.IsNullOrWhiteSpace(papCode))
            throw new UacsIncompleteException("UACS is incomplete: PAP code is required.");
        if (string.IsNullOrWhiteSpace(locationCode))
            throw new UacsIncompleteException("UACS is incomplete: location code is required.");
        if (string.IsNullOrWhiteSpace(objectAccountCode))
            throw new UacsIncompleteException("UACS is incomplete: object account code is required.");

        FundingSource = fundingSource;
        PapCode = papCode;
        LocationCode = locationCode;
        ExpenseClass = expenseClass;
        ObjectAccountCode = objectAccountCode;
    }

    /// <summary>The fund cluster, reached only through the funding source.</summary>
    public FundCluster Cluster => FundingSource.Cluster;
}
