namespace CvSU.Ais.Api.Auth;

/// <summary>
/// Configuration for trusting Frappe-issued OAuth tokens (the "Frappe issues, service trusts"
/// model — see cvsu-ais-desktop/docs/SSO_DESIGN.md). Bound from the <c>FrappeAuth</c> section.
///
/// When <see cref="Enabled"/> is false the service stays on the dev header scheme; this keeps
/// the Postman collection and offline desktop dev working until the SSO flow is fully wired.
/// </summary>
public sealed class FrappeAuthOptions
{
    public const string SectionName = "FrappeAuth";

    /// <summary>Turn the Frappe-introspection scheme on. False ⇒ DevHeader remains the default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Frappe base URL (the OAuth issuer), e.g. <c>http://accounting.localhost:8002</c>.
    /// The introspection endpoint is derived as
    /// <c>{Authority}/api/method/frappe.integrations.oauth2.introspect_token</c>.</summary>
    public string Authority { get; set; } = "";

    /// <summary>The registered OAuth Client id (the desktop public client). Sent with the
    /// introspection request; Frappe accepts client credentials in the body.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>The client secret, if the introspection endpoint requires client auth. Optional
    /// for a public client; leave empty when not needed.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>How long to cache a positive introspection result (seconds). The token TTL is
    /// ~1h, so a short cache (default 60s) bounds the per-request round-trip without letting a
    /// revoked token linger long. Set 0 to disable caching.</summary>
    public int CacheSeconds { get; set; } = 60;
}
