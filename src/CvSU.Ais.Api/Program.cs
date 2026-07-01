using System.Text.Json.Serialization;
using CvSU.Ais.Api.Auth;
using CvSU.Ais.Api.ErrorHandling;
using CvSU.Ais.Application;
using CvSU.Ais.Domain.Budget;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

const string SpaCorsPolicy = "spa";

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from configuration (env var / user-secrets). A well-known
// local default is used only in Development so nobody has to configure a throwaway DB;
// any other environment must supply it explicitly rather than ship credentials in source.
var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = builder.Environment.IsDevelopment()
        ? "Host=localhost;Port=5432;Database=cvsu_ais;Username=postgres;Password=postgres"
        : throw new InvalidOperationException(
            "ConnectionStrings:Postgres is not configured. Provide it via environment variable " +
            "or user-secrets (do not commit credentials to appsettings.json).");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

// Authentication. Two schemes coexist:
//   • DevHeader  — reads X-User / X-Roles (local dev, Postman, offline UI work).
//   • Frappe     — validates a Frappe-issued opaque Bearer token via introspection +
//                  userinfo (real SSO; "Frappe issues, the service trusts").
// FrappeAuth:Enabled picks the default: true ⇒ Frappe (production/SSO), false ⇒ DevHeader.
var frappeAuthEnabled = builder.Configuration.GetValue("FrappeAuth:Enabled", false);
var defaultScheme = frappeAuthEnabled
    ? FrappeIntrospectionAuthenticationHandler.SchemeName
    : DevHeaderAuthenticationHandler.SchemeName;

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(FrappeIntrospectionAuthenticationHandler.HttpClientName);

builder.Services
    .AddAuthentication(defaultScheme)
    .AddScheme<AuthenticationSchemeOptions, DevHeaderAuthenticationHandler>(
        DevHeaderAuthenticationHandler.SchemeName, _ => { })
    .AddScheme<FrappeAuthOptions, FrappeIntrospectionAuthenticationHandler>(
        FrappeIntrospectionAuthenticationHandler.SchemeName,
        opts => builder.Configuration.GetSection("FrappeAuth").Bind(opts));

// One policy per DV transition, each bound to the role the domain also enforces.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(DvPolicies.Create, p => p.RequireRole(DvRoles.Encoder))
    .AddPolicy(DvPolicies.RequestIaAudit, p => p.RequireRole(DvRoles.Encoder))
    .AddPolicy(DvPolicies.Submit, p => p.RequireRole(DvRoles.Encoder))
    .AddPolicy(DvPolicies.Approve, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(DvPolicies.ApproveForPayment, p => p.RequireRole(DvRoles.HeadOfAgency))
    .AddPolicy(DvPolicies.Post, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(DvPolicies.Release, p => p.RequireRole(DvRoles.Treasury))
    .AddPolicy(DvPolicies.Close, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(DvPolicies.Reject, p => p.RequireRole(DvRoles.Accountant, DvRoles.HeadOfAgency))
    .AddPolicy(BudgetPolicies.Manage, p => p.RequireRole(BudgetRoles.BudgetOfficer))
    .AddPolicy(ReportPolicies.View, p => p.RequireRole(
        BudgetRoles.BudgetOfficer, DvRoles.Accountant, DvRoles.HeadOfAgency))
    // Journal Entries — every mutation posts to (or affects) the accrual GL.
    .AddPolicy(JePolicies.Create, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(JePolicies.Approve, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(JePolicies.Post, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(JePolicies.Cancel, p => p.RequireRole(DvRoles.Accountant))
    // Obligations (ORS/BURS) + NCA — encoder drafts, Budget Office verifies funds.
    .AddPolicy(ObligationPolicies.Create, p => p.RequireRole(DvRoles.Encoder))
    .AddPolicy(ObligationPolicies.Submit, p => p.RequireRole(DvRoles.Encoder))
    .AddPolicy(ObligationPolicies.Review, p => p.RequireRole(BudgetRoles.BudgetOfficer))
    .AddPolicy(ObligationPolicies.FundVerify, p => p.RequireRole(BudgetRoles.BudgetOfficer))
    .AddPolicy(ObligationPolicies.Sign, p => p.RequireRole(BudgetRoles.BudgetOfficer))
    .AddPolicy(ObligationPolicies.Cancel, p => p.RequireRole(BudgetRoles.BudgetOfficer, DvRoles.Encoder))
    .AddPolicy(ObligationPolicies.ManageNca, p => p.RequireRole(BudgetRoles.BudgetOfficer))
    // Compliance (BIR 2307 / Withholding Tax / COA).
    .AddPolicy(CompliancePolicies.Record, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(CompliancePolicies.Approve, p => p.RequireRole(DvRoles.Accountant, DvRoles.HeadOfAgency))
    .AddPolicy(CompliancePolicies.Post, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(CompliancePolicies.Settle, p => p.RequireRole(DvRoles.Accountant, DvRoles.HeadOfAgency))
    // Payroll + salary tranches — posting hits the accrual GL.
    .AddPolicy(PayrollPolicies.Manage, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(PayrollPolicies.Post, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(PayrollPolicies.ManageTranche, p => p.RequireRole(DvRoles.Accountant, BudgetRoles.BudgetOfficer))
    // Cash advances + liquidation.
    .AddPolicy(CashAdvancePolicies.Manage, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(CashAdvancePolicies.Disburse, p => p.RequireRole(DvRoles.Treasury, DvRoles.Accountant))
    .AddPolicy(CashAdvancePolicies.Post, p => p.RequireRole(DvRoles.Accountant))
    // Batch payments / transmittals (LDDAP-ADA, DV Transmittal, Audit Intake).
    .AddPolicy(PaymentPolicies.LddapManage, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(PaymentPolicies.LddapApprove, p => p.RequireRole(DvRoles.Accountant))
    .AddPolicy(PaymentPolicies.LddapTransmit, p => p.RequireRole(DvRoles.Treasury))
    .AddPolicy(PaymentPolicies.Transmittal, p => p.RequireRole(DvRoles.Encoder, DvRoles.Treasury))
    .AddPolicy(PaymentPolicies.AuditIntake, p => p.RequireRole(DvRoles.Accountant))
    // Collections (Order of Payment / Official Receipt / RCD) — Cashier function.
    .AddPolicy(CollectionsPolicies.Record, p => p.RequireRole(DvRoles.Treasury))
    // Exports (FinDES / bank reconciliation).
    .AddPolicy(ExportPolicies.Manage, p => p.RequireRole(DvRoles.Accountant, DvRoles.Treasury));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// CORS: in Development the SPA runs on a different origin and sends X-User / X-Roles
// headers with no cookies, so any-origin is safe. Outside Development, restrict to an
// explicit allow-list from configuration (Cors:AllowedOrigins) so a permissive policy
// never ships to production/SSO.
builder.Services.AddCors(options =>
    options.AddPolicy(SpaCorsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    }));

var app = builder.Build();

app.UseExceptionHandler();

// Apply migrations automatically only in Development (or when explicitly opted in via
// Database:MigrateOnStartup). In production, schema changes should be a controlled deploy
// step, not an unconditional side effect of every process start.
if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await ApplyMigrationsAsync(app);
}

// Dev convenience: seed a balanced set of GL entries so the financial statements and
// registries show real figures. Idempotent — only seeds an empty ledger.
if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    await seedScope.ServiceProvider.GetRequiredService<CvSU.Ais.Infrastructure.DevDataSeeder>()
        .SeedAsync();
}

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
