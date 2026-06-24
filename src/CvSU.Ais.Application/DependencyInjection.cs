using CvSU.Ais.Application.Budget;
using CvSU.Ais.Application.DisbursementVouchers;
using CvSU.Ais.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace CvSU.Ais.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<DisbursementVoucherService>();
        services.AddScoped<BudgetExecutionService>();
        services.AddScoped<ReportingService>();
        return services;
    }
}
