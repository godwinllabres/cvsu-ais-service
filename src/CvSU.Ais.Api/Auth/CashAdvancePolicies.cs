namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for cash advances and their liquidation.
/// Disbursement/settlement move cash and post to the GL, so they are gated to
/// Treasury/Accountant rather than any authenticated user.</summary>
public static class CashAdvancePolicies
{
    public const string Manage = "cash-advance:manage";
    public const string Disburse = "cash-advance:disburse";
    public const string Post = "liquidation:post";
}
