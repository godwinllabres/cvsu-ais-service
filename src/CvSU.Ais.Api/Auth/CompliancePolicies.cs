namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for tax/audit compliance documents
/// (BIR 2307, Withholding Tax statements, COA cases). Recording is an accountant
/// task; approval/settlement may also be exercised by the Head of Agency.</summary>
public static class CompliancePolicies
{
    public const string Record = "compliance:record";
    public const string Approve = "compliance:approve";
    public const string Post = "compliance:post";
    public const string Settle = "compliance:settle";
}
