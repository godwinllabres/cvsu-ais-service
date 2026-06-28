using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CvSU.Ais.Api.Auth;

/// <summary>
/// Validates a Frappe-issued opaque OAuth2 access token (the "Frappe issues, the service
/// trusts" model). On each request it reads <c>Authorization: Bearer &lt;token&gt;</c>, POSTs
/// it to Frappe's introspection endpoint, and — if <c>active</c> — fetches the caller's
/// email + roles from Frappe's userinfo endpoint. It then emits the SAME claims the dev
/// handler did (<see cref="ClaimTypes.Name"/> = email, one <see cref="ClaimTypes.Role"/> per
/// Frappe role), so the existing <c>[Authorize(Policy=…)]</c> policies and the domain
/// <c>TransitionContext</c> work unchanged.
///
/// Positive validations are cached per-token for <see cref="FrappeAuthOptions.CacheSeconds"/>
/// to bound the per-request round-trip latency. Uses the Web SDK's
/// <see cref="IHttpClientFactory"/> + <see cref="IMemoryCache"/> — no new NuGet.
/// </summary>
public sealed class FrappeIntrospectionAuthenticationHandler(
    IOptionsMonitor<FrappeAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
    : AuthenticationHandler<FrappeAuthOptions>(options, logger, encoder)
{
    public const string SchemeName = "Frappe";
    public const string HttpClientName = "FrappeAuth";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? authorization = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        // Cache by a hash of the token (never cache the raw token as a key).
        var cacheKey = "frappe-auth:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (cache.TryGetValue(cacheKey, out AuthenticationTicket? cached) && cached is not null)
            return AuthenticateResult.Success(cached);

        try
        {
            var http = httpClientFactory.CreateClient(HttpClientName);

            // 1) Introspect: is the token active?
            var active = await IntrospectAsync(http, token);
            if (!active)
                return AuthenticateResult.Fail("Frappe introspection: token is not active.");

            // 2) UserInfo: the reliable source of email + roles.
            var (email, roles) = await GetUserInfoAsync(http, token);
            if (string.IsNullOrWhiteSpace(email))
                return AuthenticateResult.Fail("Frappe userinfo returned no subject.");

            var claims = new List<Claim> { new(ClaimTypes.Name, email) };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

            cache.Set(cacheKey, ticket, TimeSpan.FromSeconds(Math.Max(1, Options.CacheSeconds)));
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Frappe token validation failed.");
            return AuthenticateResult.Fail("Frappe token validation error.");
        }
    }

    private async Task<bool> IntrospectAsync(HttpClient http, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url(Options.IntrospectEndpoint))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = "access_token",
            }),
        };
        ApplyHost(request);

        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Frappe introspection returned {Status}.", (int)response.StatusCode);
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.TryGetProperty("active", out var activeEl)
            && activeEl.ValueKind == JsonValueKind.True;
    }

    private async Task<(string? Email, IReadOnlyList<string> Roles)> GetUserInfoAsync(HttpClient http, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Url(Options.UserInfoEndpoint));
        request.Headers.Authorization = new("Bearer", token);
        ApplyHost(request);

        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Frappe userinfo returned {Status}.", (int)response.StatusCode);
            return (null, []);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString()
            : root.TryGetProperty("sub", out var subEl) ? subEl.GetString()
            : null;

        var roles = new List<string>();
        if (root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
        {
            roles.AddRange(rolesEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return (email, roles);
    }

    private string Url(string endpoint) =>
        endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : Options.Authority.TrimEnd('/') + "/" + endpoint.TrimStart('/');

    /// <summary>A single-site bench may resolve the OAuth machinery only under the site host;
    /// send it explicitly when configured.</summary>
    private void ApplyHost(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(Options.SiteHost))
            request.Headers.Host = Options.SiteHost;
    }
}
