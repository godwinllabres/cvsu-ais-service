using Microsoft.AspNetCore.Authentication;

namespace CvSU.Ais.Api.Auth;

/// <summary>
/// Options for the Frappe token-introspection authentication scheme. The desktop client
/// signs in via Frappe's OAuth2/OIDC provider and sends the resulting <b>opaque</b>
/// access token as <c>Authorization: Bearer</c>. Because the token is opaque (no JWKS,
/// the id_token is HS256), the service validates it by calling Frappe's introspection
/// endpoint, then reads the caller's roles from Frappe's userinfo endpoint.
/// </summary>
public sealed class FrappeAuthOptions : AuthenticationSchemeOptions
{
    /// <summary>Base URL of the Frappe site, e.g. <c>http://localhost:8000</c>.</summary>
    public string Authority { get; set; } = "http://localhost:8000";

    /// <summary>The Host header to send (the bench routes plain localhost to the site,
    /// but a single-site bench may require the site's host). Optional.</summary>
    public string? SiteHost { get; set; }

    /// <summary>The desktop OAuth client id (sent on introspection for context). Optional.</summary>
    public string? ClientId { get; set; }

    /// <summary>Frappe's token-introspection endpoint (absolute, or relative to Authority).</summary>
    public string IntrospectEndpoint { get; set; } =
        "/api/method/frappe.integrations.oauth2.introspect_token";

    /// <summary>Frappe's OpenID userinfo endpoint — the reliable source of email + roles
    /// (introspection only carries them when the user has a 'frappe' social-login record).</summary>
    public string UserInfoEndpoint { get; set; } =
        "/api/method/frappe.integrations.oauth2.openid_profile";

    /// <summary>How long to cache a positive validation (token TTL is ~1h; a short cache
    /// bounds the per-request introspection latency without holding a revoked token long).</summary>
    public int CacheSeconds { get; set; } = 60;
}
