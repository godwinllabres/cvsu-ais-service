using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CvSU.Ais.Contracts;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Disbursement;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>
/// Exercises the HTTP authorization boundary end-to-end: the per-transition policies
/// (DvPolicies → DvRoles) and the certify endpoint's domain-enforced role check, through
/// the real ASP.NET pipeline (dev-header auth scheme). This is the layer the service-level
/// tests bypass, and it is exactly what the recent Frappe role rename put in flux —
/// a drift between the X-Roles names and the policy role names would silently 401/403.
/// The API boots against the shared Postgres test container.
/// </summary>
[Collection("postgres")]
public sealed class ApiAuthorizationTests : IDisposable
{
    private const string Dvs = "/api/disbursement-vouchers";

    // The API serialises enums as strings (JsonStringEnumConverter), so the client must too.
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly WebApplicationFactory<Program> _factory;

    public ApiAuthorizationTests(PostgresFixture fixture)
    {
        // Program reads the connection string and FrappeAuth:Enabled DURING CreateBuilder,
        // before Build() — so WithWebHostBuilder config layers in too late. Environment
        // variables are added by CreateBuilder itself and override appsettings.json, so set
        // them here (before the host builds on first CreateClient): point at the test
        // container, and force the dev-header scheme so we can assert the role→policy wiring
        // via X-Roles (appsettings.json turns on the Frappe token scheme, which ignores it).
        // Development is required for the dev-header scheme (FrappeAuth:Enabled=false) — the
        // host fail-fasts on that combination outside Development.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", fixture.ConnectionString);
        Environment.SetEnvironmentVariable("FrappeAuth__Enabled", "false");
        _factory = new WebApplicationFactory<Program>();
    }

    private HttpClient Client(string? user = null, params string[] roles)
    {
        var client = _factory.CreateClient();
        if (user is not null) client.DefaultRequestHeaders.Add("X-User", user);
        if (roles.Length > 0) client.DefaultRequestHeaders.Add("X-Roles", string.Join(",", roles));
        return client;
    }

    private const string Admin = "System Manager"; // a TransitionContext.AdminRoles member

    private static readonly object MinimalDv = new
    {
        fiscalYear = 2026,
        amount = 100,
        fundingSourceCode = "01101101",
    };

    // A fully-encoded DV (complete UACS line) so it can pass the encoding-complete gate.
    private static readonly object FullDv = new
    {
        fiscalYear = 2026,
        amount = 100,
        dvType = "Others",
        fundingSourceCode = "01101101",
        papCode = "PAP-A",
        locationCode = "LOC-A",
        expenseClass = "Mooe",
        objectAccountCode = "50203010",
    };

    [Fact]
    public async Task Unauthenticated_caller_is_challenged()
    {
        var res = await Client().PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Create_without_the_encoder_role_is_forbidden()
    {
        var res = await Client("nobody@cvsu").PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Create_with_the_encoder_role_succeeds()
    {
        var res = await Client("clerk@cvsu", DvRoles.Encoder).PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Edit_without_the_encoder_role_is_forbidden()
    {
        // Authorization runs before the action, so the DV need not exist to prove the gate.
        var res = await Client("nobody@cvsu").PutAsJsonAsync($"{Dvs}/DV-2026-00001", MinimalDv);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Approve_without_the_accountant_role_is_forbidden()
    {
        var res = await Client("nobody@cvsu").PostAsync($"{Dvs}/DV-2026-00001/approve", null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Record_payment_without_the_cashier_role_is_forbidden()
    {
        var res = await Client("nobody@cvsu").PostAsJsonAsync(
            $"{Dvs}/DV-2026-00001/payment", new { method = "Cheque", reference = "X" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Certify_with_the_wrong_role_is_forbidden_but_the_responsible_officer_succeeds()
    {
        // The certify endpoint is authenticated-only; the per-certification role is enforced
        // by the aggregate, so we need a real DV to reach that check.
        var created = await (await Client("clerk@cvsu", DvRoles.Encoder)
            .PostAsJsonAsync(Dvs, MinimalDv)).Content.ReadFromJsonAsync<DvStateView>(Json);
        var name = created!.Name;

        // The encoder cannot assert the Budget Office certification → 403.
        var wrong = await Client("clerk@cvsu", DvRoles.Encoder).PostAsJsonAsync(
            $"{Dvs}/{name}/certify", new { certification = "BudgetSufficiency" });
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);

        // The Budget Officer can → 200.
        var ok = await Client("budget@cvsu", BudgetRoles.BudgetOfficer).PostAsJsonAsync(
            $"{Dvs}/{name}/certify", new { certification = "BudgetSufficiency" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Administrator_satisfies_a_role_policy_without_holding_the_functional_role()
    {
        // Holds ONLY System Manager — no Encoder — yet the admin-aware policy lets it create.
        var res = await Client("boss@cvsu", Admin).PostAsJsonAsync(Dvs, MinimalDv);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_drive_a_dv_end_to_end_including_approving_their_own()
    {
        var admin = Client("boss@cvsu", Admin);

        var created = await (await admin.PostAsJsonAsync(Dvs, FullDv))
            .Content.ReadFromJsonAsync<DvStateView>(Json);
        var name = created!.Name;

        // Admin certifies every box (the domain treats it as holding each cert role).
        foreach (var cert in new[]
                 { "BudgetSufficiency", "InternalAudit", "EndUserAcceptance", "AccountantSignature", "SupplyPropertyInspection" })
            Assert.Equal(HttpStatusCode.OK,
                (await admin.PostAsJsonAsync($"{Dvs}/{name}/certify", new { certification = cert })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{Dvs}/{name}/request-ia-audit", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"{Dvs}/{name}/submit", null)).StatusCode);

        // The SAME user encoded AND approves — allowed only by the admin SoD escape hatch.
        var approved = await admin.PostAsync($"{Dvs}/{name}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var view = await approved.Content.ReadFromJsonAsync<DvStateView>(Json);
        Assert.Equal(DvWorkflowStatus.Approved, view!.Status);
        Assert.Matches(@"^DV-CN-01-2026-\d{5}$", view.ControlNumber!);
    }

    public void Dispose() => _factory.Dispose();
}
