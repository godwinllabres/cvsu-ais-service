using CvSU.Ais.Application.DisbursementVouchers;
using Microsoft.Extensions.DependencyInjection;

namespace CvSU.Ais.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<DisbursementVoucherService>();
        return services;
    }
}
