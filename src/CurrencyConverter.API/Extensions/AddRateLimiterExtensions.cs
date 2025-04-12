using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CurrencyConverter.API.Extensions;

public static class AddRateLimiterExtensions
{
    public static IServiceCollection AddRateLimiterService(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", options =>
            {
                options.PermitLimit = 100;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0; //no queueing
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        return services;
    }
}