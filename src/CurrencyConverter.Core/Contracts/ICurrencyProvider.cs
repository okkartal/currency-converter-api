using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

public interface ICurrencyProvider
{
    string ProviderName { get; }
    Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);

    Task<ExchangeRate> GetRateForDateAsync(string baseCurrency, DateTime date,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ExchangeRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default);
}