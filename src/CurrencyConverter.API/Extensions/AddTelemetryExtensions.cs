using OpenTelemetry.Trace;

namespace CurrencyConverter.API.Extensions;

public static class AddTelemetryExtensions
{
    public static IServiceCollection AddTelemetryService(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });
        return services;
    }
}