using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CvSU.Ais.Infrastructure.Design;

/// <summary>
/// Lets the <c>dotnet ef</c> tooling construct the context at design time without
/// booting the API host. The connection string here is only used to pick the
/// provider for migration scaffolding — no database connection is opened when
/// generating migrations.
/// </summary>
public sealed class AisDbContextFactory : IDesignTimeDbContextFactory<AisDbContext>
{
    public AisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AisDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cvsu_ais;Username=postgres;Password=postgres")
            .Options;

        return new AisDbContext(options);
    }
}
