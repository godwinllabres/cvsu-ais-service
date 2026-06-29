using CvSU.Ais.Infrastructure;
using CvSU.Ais.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CvSU.Ais.Integration.Tests;

/// <summary>
/// Starts a real PostgreSQL container once for the whole integration suite and
/// applies the EF migrations to it. Every context it hands out has the ledger
/// immutability interceptor wired, exactly as production does.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    /// <summary>The container's connection string, so the WebApplicationFactory-based
    /// HTTP tests boot the real API against this same isolated database.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public AisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AisDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new LedgerImmutabilityInterceptor())
            .Options;
        return new AisDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>Shares one container across all integration test classes.</summary>
[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
