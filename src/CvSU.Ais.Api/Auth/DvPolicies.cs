namespace CvSU.Ais.Api.Auth;

/// <summary>Authorization policy names, one per DV transition. Each is bound to
/// the same role the domain transition requires, so the HTTP boundary rejects a
/// wrong-role caller before the request ever reaches the aggregate.</summary>
public static class DvPolicies
{
    public const string Create = "dv:create";
    public const string RequestIaAudit = "dv:request-ia-audit";
    public const string Submit = "dv:submit";
    public const string Approve = "dv:approve";
    public const string ApproveForPayment = "dv:approve-for-payment";
    public const string Post = "dv:post";
    public const string Release = "dv:release";
    public const string Close = "dv:close";
    public const string Reject = "dv:reject";
}
