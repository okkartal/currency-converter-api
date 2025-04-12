using CurrencyConverter.Infrastructure.Services;
using Polly;

namespace CurrencyConverter.API.Extensions;

public static class AddHttpClientExtensions
{
    public static IServiceCollection AddHttpClientService(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri(configuration["BaseAddress"] ?? "https://api.frankfurter.dev/v1/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outCome, timeSpan, retryAttempt, context) =>
                {
                    Console.WriteLine(
                        $"Retrying HTTP request attempt {retryAttempt} after {context.PolicyKey} cue to {outCome.Result?.StatusCode}");
                }));
        return services;
    }
}