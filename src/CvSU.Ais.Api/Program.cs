using System.Security.Claims;
using System.Text.Json.Serialization;
using CvSU.Ais.Api.Auth;
using CvSU.Ais.Api.ErrorHandling;
using CvSU.Ais.Application;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

const string SpaCorsPolicy = "spa";

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Pin the content root to the EXE's own directory so appsettings.json is always loaded
    // from beside the binary, regardless of the launcher's working directory. Without this,
    // launching the exe from any other CWD silently skips appsettings (e.g. FrappeAuth:Enabled
    // defaults to false → the service drops back to dev-header auth and rejects Bearer tokens).
    ContentRootPath = AppContext.BaseDirectory,
});

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=cvsu_ais;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

// Authentication. Two schemes are registered; the DEFAULT is chosen by config:
//   • FrappeAuth:Enabled = true  → trust Frappe-issued OAuth tokens (introspection). Production.
//   • FrappeAuth:Enabled = false → dev header (X-User/X-Roles). Local dev, Postman, offline desktop.
// Both stay registered so flipping the switch needs no code change. See SSO_DESIGN.md.
builder.Services.Configure<FrappeAuthOptions>(
    builder.Configuration.GetSection(FrappeAuthOptions.SectionName));
var frappeAuthEnabled = builder.Configuration
    .GetSection(FrappeAuthOptions.SectionName).GetValue<bool>(nameof(FrappeAuthOptions.Enabled));

// Hardening: the dev-header scheme trusts client-supplied X-Roles verbatim (a caller can
// self-grant any role, including the System Manager / Administrator escape hatch). It must
// therefore NEVER be the active scheme outside Development. Fail fast at startup rather than
// silently drop a misconfigured staging/production instance into "trust any role" mode — the
// exact footgun the FrappeAuth comment above warns about.
if (!frappeAuthEnabled && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        $"Dev-header authentication ({FrappeAuthOptions.SectionName}:{nameof(FrappeAuthOptions.Enabled)}=false) is " +
        $"only permitted in the Development environment (current: '{builder.Environment.EnvironmentName}'). " +
        $"Set {FrappeAuthOptions.SectionName}:{nameof(FrappeAuthOptions.Enabled)}=true to use Frappe token introspection.");

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(FrappeIntrospectionAuthenticationHandler.SchemeName);

builder.Services
    .AddAuthentication(frappeAuthEnabled
        ? FrappeIntrospectionAuthenticationHandler.SchemeName
        : DevHeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevHeaderAuthenticationHandler>(
        DevHeaderAuthenticationHandler.SchemeName, _ => { })
    .AddScheme<AuthenticationSchemeOptions, FrappeIntrospectionAuthenticationHandler>(
        FrappeIntrospectionAuthenticationHandler.SchemeName, _ => { });

// A policy is satisfied by the listed role(s) OR by an administrator role — the SoD escape
// hatch, giving a Frappe System Manager / Administrator full access (the domain grants the
// same via TransitionContext.IsAdministrator, so the two layers stay consistent). SECURITY:
// the bypass is keyed on the ROLE (TransitionContext.AdminRoles), never the username — the
// dev-header scheme lets a caller assert any username, so a username check would be trivially
// forgeable. Authentication is still required, exactly as RequireRole demands.
static Action<AuthorizationPolicyBuilder> RoleOrAdmin(params string[] roles) =>
    policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
        {
            // Case-insensitive, matching the domain's TransitionContext (OrdinalIgnoreCase) —
            // ctx.User.IsInRole is ordinal/case-sensitive, which would make the HTTP gate and
            // the domain disagree on role casing.
            var held = new HashSet<string>(
                ctx.User.FindAll(ClaimTypes.Role).Select(c => c.Value), StringComparer.OrdinalIgnoreCase);
            return roles.Any(held.Contains) || TransitionContext.AdminRoles.Any(held.Contains);
        });

// One policy per DV transition, each bound to the role the domain also enforces.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(DvPolicies.Create, RoleOrAdmin(DvRoles.Encoder))
    .AddPolicy(DvPolicies.Edit, RoleOrAdmin(DvRoles.Encoder))
    .AddPolicy(DvPolicies.RequestIaAudit, RoleOrAdmin(DvRoles.Encoder))
    // "Submit to Accounting" and "Return to Clerk" are the Internal Auditor's calls.
    .AddPolicy(DvPolicies.Submit, RoleOrAdmin(DvRoles.InternalAuditor))
    .AddPolicy(DvPolicies.ReturnToClerk, RoleOrAdmin(DvRoles.InternalAuditor))
    .AddPolicy(DvPolicies.Approve, RoleOrAdmin(DvRoles.Accountant))
    .AddPolicy(DvPolicies.ApproveForPayment, RoleOrAdmin(DvRoles.HeadOfAgency))
    .AddPolicy(DvPolicies.Post, RoleOrAdmin(DvRoles.Accountant))
    .AddPolicy(DvPolicies.Release, RoleOrAdmin(DvRoles.Treasury))
    // The cashier captures the cheque/ADA/transfer reference around release.
    .AddPolicy(DvPolicies.RecordPayment, RoleOrAdmin(DvRoles.Treasury))
    .AddPolicy(DvPolicies.Close, RoleOrAdmin(DvRoles.Accountant))
    .AddPolicy(BudgetPolicies.Manage, RoleOrAdmin(BudgetRoles.BudgetOfficer))
    .AddPolicy(ReportPolicies.View, RoleOrAdmin(
        BudgetRoles.BudgetOfficer, DvRoles.Accountant, DvRoles.HeadOfAgency))
    // Collections are recorded by the Cashier / Collecting Officer.
    .AddPolicy(CollectionsPolicies.Record, RoleOrAdmin(DvRoles.Treasury));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Dev CORS: the SPA runs on a different origin and sends X-User / X-Roles headers.
// No cookies are used, so any-origin is safe here; tighten for production.
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

await ApplyMigrationsAsync(app);

app.UseCors(SpaCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Applies pending migrations on startup, retrying while Postgres comes up under
// Docker Compose (the API container can start before the database is ready).
static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AisDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (attempt < 10)
        {
            app.Logger.LogWarning(ex, "Database not ready (attempt {Attempt}/10); retrying in 3s.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
