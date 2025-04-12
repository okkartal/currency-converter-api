using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CurrencyConverter.Core.Models;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri("https://api.frankfurter.app/");
    }

    public async Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"latest?base={baseCurrency}";

            var response =
                await _httpClient.GetFromJsonAsync<ApiResponse>(url, JsonOptions, cancellationToken);
            return response is null
                ? throw new InvalidOperationException("Failed to get latest rates.")
                : MapToExchangeRate(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting latest rates for {baseCurrency}");
            throw;
        }
    }

    public async Task<ExchangeRate> GetRateForDateAsync(string baseCurrency, DateTime date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateString = date.ToString("yyyy-MM-dd");

            var url = $"{dateString}?base={baseCurrency}";

            var response =
                await _httpClient.GetFromJsonAsync<ApiResponse>(url, JsonOptions, cancellationToken);
            return response is null
                ? throw new InvalidOperationException("Failed to get rate for date.")
                : MapToExchangeRate(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting currency rates for {baseCurrency} on {date}");
            throw;
        }
    }

    public async Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate,
        DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var startDateString = startDate.ToString("yyyy-MM-dd");
            var endDateString = endDate.ToString("yyyy-MM-dd");

            var url = $"{startDateString}..{endDateString}?base={baseCurrency ?? "EUR"}";

            var response = await _httpClient.GetFromJsonAsync<TimeSeriesResponse>(
                url, cancellationToken);
            return response is null
                ? throw new InvalidOperationException("Failed to get historical rates.")
                : MapToExchangeRate(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting historical rates for {baseCurrency} from {startDate} to {endDate}");
            throw;
        }
    }

    private ExchangeRate MapToExchangeRate(ApiResponse response)
    {
        return new ExchangeRate
        {
            Base = response.Base,
            Date = response.Date,
            Rates = response.Rates
        };
    }

    private IEnumerable<ExchangeRate> MapToExchangeRate(TimeSeriesResponse response)
    {
        return response.Rates.Select(entry => new ExchangeRate
        {
            Base = response.Base,
            Date = entry.Key,
            Rates = entry.Value
        });
    }

    private record ApiResponse(decimal Amount, string Base, DateTime Date, Dictionary<string, decimal> Rates);

    private record TimeSeriesResponse(
        decimal Amount,
        string Base,
        DateTime StartDate,
        DateTime EndDate,
        Dictionary<DateTime, Dictionary<string, decimal>> Rates);
}