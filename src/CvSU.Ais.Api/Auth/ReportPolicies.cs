namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy for the read-only financial registries. Any of
/// the financial roles may view; reports never mutate state.</summary>
public static class ReportPolicies
{
    public const string View = "report:view";
}
