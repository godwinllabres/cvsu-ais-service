using CvSU.Ais.Api.Auth;
using CvSU.Ais.Api.ErrorHandling;
using CvSU.Ais.Application;
using CvSU.Ais.Domain.Disbursement;
using CvSU.Ais.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=cvsu_ais;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

builder.Services
    .AddAuthentication(DevHeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DevHeaderAuthenticationHandler>(
        DevHeaderAuthenticationHandler.SchemeName, _ => { });

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
    .AddPolicy(DvPolicies.Reject, p => p.RequireRole(DvRoles.Accountant, DvRoles.HeadOfAgency));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

await ApplyMigrationsAsync(app);

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
