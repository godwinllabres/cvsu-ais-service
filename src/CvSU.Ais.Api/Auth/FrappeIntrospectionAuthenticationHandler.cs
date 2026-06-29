using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CvSU.Ais.Api.Auth;

/// <summary>
/// Authenticates a caller by validating their <c>Authorization: Bearer</c> token against Frappe's
/// OAuth2 <b>introspection</b> endpoint, then projecting the result into the SAME claims the dev
/// header scheme emits (<see cref="ClaimTypes.Name"/> = email, one <see cref="ClaimTypes.Role"/>
/// per Frappe role). Because the controllers and the domain <c>TransitionContext</c> read only
/// those two claim types, this handler is a drop-in for <see cref="DevHeaderAuthenticationHandler"/>.
///
/// Why introspection and not <c>AddJwtBearer</c>: Frappe's access_token is an OPAQUE random string
/// (the id_token is HS256 with the client secret, and there is NO JWKS), so it cannot be validated
/// locally by a resource server that doesn't hold the secret. The three-party "Frappe issues,
/// service trusts" model therefore REQUIRES introspection. The roles come back in the same call.
///
/// Positive results are cached briefly (<see cref="FrappeAuthOptions.CacheSeconds"/>) so we don't
/// round-trip to Frappe on every request; the token TTL (~1h) bounds how stale a cached grant can be.
/// </summary>
public sealed class FrappeIntrospectionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<FrappeAuthOptions> frappeOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Frappe";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly FrappeAuthOptions _opts = frappeOptions.Value;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            // No bearer token — let the pipeline treat it as unauthenticated (401), not an error.
            return AuthenticateResult.NoResult();
        }

        var token = header.Parameter!;

        try
        {
            var claims = await ResolveClaimsAsync(token);
            if (claims is null)
                return AuthenticateResult.Fail("Token is inactive or was rejected by Frappe.");

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Frappe token introspection failed.");
            return AuthenticateResult.Fail("Could not validate the token with Frappe.");
        }
    }

    /// <summary>Returns the claims for a valid token, or null if Frappe reports it inactive.
    /// Caches the positive projection for <see cref="FrappeAuthOptions.CacheSeconds"/>.</summary>
    private async Task<IReadOnlyList<Claim>?> ResolveClaimsAsync(string token)
    {
        var cacheKey = $"frappe-introspect:{token}";
        if (_opts.CacheSeconds > 0 && cache.TryGetValue(cacheKey, out IReadOnlyList<Claim>? cached))
            return cached;

        var result = await IntrospectAsync(token);
        if (result is not { Active: true })
            return null;

        // Frappe enriches the introspection response with the OpenID claims (sub, email, roles)
        // ONLY when "openid" is in the token scope AND the user has a frappe-provider
        // "User Social Login" row (see frappe/integrations/oauth2.py:introspect_token). Users who
        // sign in through the OAuth flow get that row automatically; the bootstrap Administrator
        // does not unless seeded (patch_20260625b). If roles come back empty here, the caller
        // resolves to an unprivileged principal and role-gated endpoints return 403 — which is
        // the safe default, and the fix is to provision the social-login row, not to widen auth.
        // (The userinfo/openid_profile endpoint can't substitute: called with a plain
        // Authorization: Bearer header it raises AuthenticationError.)

        var claims = new List<Claim>();
        var name = result.Email ?? result.Sub;
        if (!string.IsNullOrWhiteSpace(name))
            claims.Add(new Claim(ClaimTypes.Name, name!));

        foreach (var role in result.Roles ?? [])
            if (!string.IsNullOrWhiteSpace(role))
                claims.Add(new Claim(ClaimTypes.Role, role));

        if (_opts.CacheSeconds > 0)
            cache.Set(cacheKey, (IReadOnlyList<Claim>)claims,
                TimeSpan.FromSeconds(_opts.CacheSeconds));

        return claims;
    }

    private async Task<IntrospectionResponse?> IntrospectAsync(string token)
    {
        var endpoint = $"{_opts.Authority.TrimEnd('/')}/api/method/frappe.integrations.oauth2.introspect_token";

        var form = new List<KeyValuePair<string, string>>
        {
            new("token", token),
            new("token_type_hint", "access_token"),
        };
        if (!string.IsNullOrWhiteSpace(_opts.ClientId))
            form.Add(new("client_id", _opts.ClientId));
        if (!string.IsNullOrWhiteSpace(_opts.ClientSecret))
            form.Add(new("client_secret", _opts.ClientSecret));

        var client = httpClientFactory.CreateClient(SchemeName);
        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(endpoint, content, Context.RequestAborted);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IntrospectionResponse>(Json, Context.RequestAborted);
    }

    /// <summary>The subset of Frappe's introspection response we consume. When the token carries
    /// the "openid" scope and the user has a frappe social-login row, Frappe also includes
    /// <c>email</c> and <c>roles</c> here (see the handler note above).</summary>
    private sealed class IntrospectionResponse
    {
        public bool Active { get; set; }
        public string? Email { get; set; }
        public string? Sub { get; set; }

        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
    }
}
