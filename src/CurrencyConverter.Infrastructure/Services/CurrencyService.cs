using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace CurrencyConverter.Infrastructure.Services;

public class CurrencyService(
    ICurrencyProviderFactory providerFactory,
    ICacheService cacheService,
    ILogger<CurrencyService> logger) : ICurrencyService
{
    private static readonly HashSet<string> RestrictedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        { "TRY", "PLN", "THB", "MXN" };

    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromMinutes(1),
            (ex, breakDuration) =>
            {
                logger.LogWarning("Circuit breaker opened for {BreakDuration}, API might be down", breakDuration);
            },
            () => logger.LogInformation("Circuit breaker reset. API is available again"),
            () => logger.LogInformation("Circuit breaker halved. Testing API availability"));

    public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseCurrency, nameof(baseCurrency));

        if (RestrictedCurrencies.Contains(baseCurrency))
            throw new InvalidOperationException($"Currency '{baseCurrency}' is restricted");

        return await cacheService.GetOrCreateAsync(
            $"base={baseCurrency}",
            async () => await ExecuteWithPolicyAsync(() =>
            {
                var provider = providerFactory.GetProvider();
                return provider.GetLatestRatesAsync(baseCurrency, cancellationToken);
            }),
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    public async Task<ConversionResult> ConvertCurrencyAsync(ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.FromCurrency, nameof(request.FromCurrency));
        ArgumentException.ThrowIfNullOrEmpty(request.ToCurrency, nameof(request.ToCurrency));

        if (RestrictedCurrencies.Contains(request.FromCurrency))
            throw new InvalidOperationException($"Currency '{request.FromCurrency}' is restricted");

        // Get latest for the 'from' currency
        var rates = await GetLatestRatesAsync(request.FromCurrency, cancellationToken);

        // Check if the 'to' currency is available in the rates
        if (!rates.Rates.TryGetValue(request.ToCurrency, out var rate))
            throw new InvalidOperationException(
                $"Exchange rate from {request.FromCurrency} to {request.ToCurrency} is not available");

        var convertedAmount = request.Amount * rate;

        return new ConversionResult(
            request.FromCurrency,
            request.ToCurrency,
            request.Amount,
            convertedAmount,
            rate,
            rates.Date);
    }

    public async Task<PaginatedResult<ExchangeRate>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.BaseCurrency, nameof(request.BaseCurrency));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.StartDate, request.EndDate, nameof(request.StartDate));

        if (RestrictedCurrencies.Contains(request.BaseCurrency))
            throw new InvalidOperationException($"Currency '{request.BaseCurrency}' is restricted");

        // Limit request timespan to protect API from abuse
        const int maxDaysAllowed = 365;
        var requestDays = (request.EndDate - request.StartDate).Days + 1;

        if (requestDays > maxDaysAllowed)
            throw new ArgumentException($"Historical data request cannot exceed {maxDaysAllowed} days");

        // Cache key includes all pagination parameters
        var cacheKey =
            $"{request.BaseCurrency}_{request.StartDate:yyyy-MM-dd}_{request.EndDate:yyyy-MM-dd}_{request.Page}_{request.PageSize}";

        return await cacheService.GetOrCreateAsync(
            cacheKey,
            async () => await ExecuteWithPolicyAsync(async () =>
            {
                var provider = providerFactory.GetProvider();

                // Get all historical rates for the date range
                var allRates = await provider.GetHistoricalRatesAsync(
                    request.BaseCurrency,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);

                var ratesList = allRates.ToList();

                // Apply pagination
                var totalCount = ratesList.Count;
                var pageSize = Math.Max(1, request.PageSize);
                var page = Math.Max(1, request.Page);
                var skip = (page - 1) * pageSize;

                var paginatedItems = ratesList
                    .OrderByDescending(r => r.Date)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                return new PaginatedResult<ExchangeRate>
                {
                    Items = paginatedItems,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }),
            TimeSpan.FromHours(1),
            cancellationToken);
    }

    // Helper method to execute operations with retry and circuit breaker policies
    private async Task<T> ExecuteWithPolicyAsync<T>(Func<Task<T>> operation)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3, // Number of retries
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                (exception, timeSpan, retryCount, _) =>
                {
                    logger.LogWarning("Retry {RetryCount} failed after {Seconds} seconds: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });

        // Combine retry and circuit breaker policies
        return await retryPolicy
            .WrapAsync(_circuitBreakerPolicy)
            .ExecuteAsync(operation);
    }
}