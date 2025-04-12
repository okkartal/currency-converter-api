namespace CurrencyConverter.API.Extensions;

public static class AddRedisExtensions
{
    public static IServiceCollection AddRedisService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "CurrencyConverter:";
        });
        return services;
    }
}