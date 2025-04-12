using CurrencyConverter.Infrastructure.Services;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure.Providers;

public class CurrencyProvider(ApiClient apiClient, ILogger<CurrencyProvider> logger) : ICurrencyProvider
{
    private readonly ApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    private readonly ILogger<CurrencyProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string? ProviderName { get; }

    public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Getting latest rates for {baseCurrency}");
        return await _apiClient.GetLatestRatesAsync(baseCurrency, cancellationToken);
    }

    public async Task<ExchangeRate> GetRateForDateAsync(string baseCurrency, DateTime date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Getting rates for {baseCurrency} on {date}");
        return await _apiClient.GetRateForDateAsync(baseCurrency, date, cancellationToken);
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Getting historical rates for {baseCurrency} from {startDate} to {endDate}");
        return await _apiClient.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, cancellationToken);
    }
}