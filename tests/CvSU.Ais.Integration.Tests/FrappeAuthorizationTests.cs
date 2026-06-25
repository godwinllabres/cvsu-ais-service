using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CvSU.Ais.Api.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>
/// Exercises the PRODUCTION default auth scheme — Frappe OAuth2 token introspection — which
/// every other auth test disables in favour of the dev header. The Frappe introspection HTTP
/// call is stubbed, so this proves the real role→ClaimTypes.Role→IsInRole projection (and the
/// admin escape hatch) work end-to-end under the scheme an as-shipped instance actually uses.
/// The SSO design doc calls for exactly this gate.
/// </summary>
[Collection("postgres")]
public sealed class FrappeAuthorizationTests : IDisposable
{
    private const string Dvs = "/api/disbursement-vouchers";
    private static readonly object MinimalDv = new { fiscalYear = 2026, amount = 100, fundingSourceCode = "01101101" };

    private readonly WebApplicationFactory<Program> _factory;

    public FrappeAuthorizationTests(PostgresFixture fixture)
    {
        // Read before Build() — so set via env vars (see ApiAuthorizationTests for why). Frappe
        // token scheme is the default here (Enabled=true), against the shared test container.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", fixture.ConnectionString);
        Environment.SetEnvironmentVariable("FrappeAuth__Enabled", "true");

        // Replace the named "Frappe" HttpClient's transport with a stub of Frappe's
        // introspect_token endpoint (ConfigureTestServices runs after the app's registrations).
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(services =>
                services.AddHttpClient(FrappeIntrospectionAuthenticationHandler.SchemeName)
                    .ConfigurePrimaryHttpMessageHandler(() => new StubIntrospection())));
    }

    private HttpClient Bearer(string? token)
    {
        var client = _factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Admin_token_satisfies_a_role_gated_endpoint_under_the_frappe_scheme()
    {
        // roles:["System Manager"] → ClaimTypes.Role → admin bypass → create allowed.
        var res = await Bearer("admin-token").PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Functional_role_token_satisfies_its_policy_under_the_frappe_scheme()
    {
        // Proves the role projection works for an ordinary role, not just admin.
        var res = await Bearer("encoder-token").PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Active_token_with_no_roles_is_forbidden()
    {
        var res = await Bearer("noroles-token").PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Inactive_token_is_unauthorized()
    {
        var res = await Bearer("anything-else").PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Missing_bearer_token_is_unauthorized()
    {
        var res = await Bearer(null).PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>Stands in for Frappe's introspect_token endpoint: returns a canned introspection
    /// response keyed on the posted token (form field "token").</summary>
    private sealed class StubIntrospection : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var form = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var token = form.Split('&')
                .Select(p => p.Split('=', 2))
                .Where(kv => kv.Length == 2 && kv[0] == "token")
                .Select(kv => Uri.UnescapeDataString(kv[1]))
                .FirstOrDefault() ?? "";

            var json = token switch
            {
                "admin-token" => """{"active":true,"email":"boss@cvsu","roles":["System Manager"]}""",
                "encoder-token" => """{"active":true,"email":"clerk@cvsu","roles":["AIS Accounting Encoder"]}""",
                "noroles-token" => """{"active":true,"email":"plain@cvsu","roles":[]}""",
                _ => """{"active":false}""",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
