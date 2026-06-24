using CvSU.Ais.Application.Abstractions;
using CvSU.Ais.Infrastructure.Interceptors;
using CvSU.Ais.Infrastructure.Numbering;
using CvSU.Ais.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CvSU.Ais.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core context (with the immutability interceptor)
    /// and the gapless voucher-number service.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<LedgerImmutabilityInterceptor>();

        services.AddDbContext<AisDbContext>((sp, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<LedgerImmutabilityInterceptor>()));

        services.AddScoped<IVoucherNumberGenerator, GaplessVoucherNumberService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFundingSourceCatalog, FundingSourceCatalog>();
        services.AddScoped<IDisbursementVoucherRepository, DisbursementVoucherRepository>();
        services.AddScoped<IBudgetLedger, BudgetLedgerRepository>();
        services.AddScoped<IGeneralLedger, GeneralLedgerRepository>();
        services.AddScoped<IReportingQueries, ReportingQueries>();

        return services;
    }
}
