using CurrencyConverter.Infrastructure.Factories;
using CurrencyConverter.Infrastructure.Providers;
using CurrencyConverter.Infrastructure.Services;
using CurrencyConverter.Core.Contracts; 

namespace CurrencyConverter.API.Extensions;

public static class AddApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtSettingService(configuration);
        services.AddRateLimiterService();
        services.AddRedisService(configuration);
        services.AddSwaggerService();
        services.AddHttpClientService(configuration);

        services.AddScoped<ICurrencyProvider, CurrencyProvider>();
        services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ICacheService, DistributedCacheService>();

        services.AddTelemetryService();
        services.AddHealthChecks();

        return services;
    }
}