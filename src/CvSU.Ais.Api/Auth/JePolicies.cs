namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for manual Journal Entries. Journal entries
/// post directly to the accrual GL, so every mutating action is gated to the
/// Accountant role at the HTTP boundary — mirroring <see cref="DvPolicies"/>.</summary>
public static class JePolicies
{
    public const string Create = "je:create";
    public const string Approve = "je:approve";
    public const string Post = "je:post";
    public const string Cancel = "je:cancel";
}
