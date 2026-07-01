namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names for the ORS/BURS obligation lifecycle and NCA.
/// Fund verification posts an obligation to the budget registry, so it is gated to the
/// Budget Officer; drafting/submitting stays with the encoder.</summary>
public static class ObligationPolicies
{
    public const string Create = "ors:create";
    public const string Submit = "ors:submit";
    public const string Review = "ors:review";
    public const string FundVerify = "ors:fund-verify";
    public const string Sign = "ors:sign";
    public const string Cancel = "ors:cancel";
    public const string ManageNca = "nca:manage";
}
