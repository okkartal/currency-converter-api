using CurrencyConverter.Core.Models;

namespace CurrencyConverter.Core.Contracts;

public interface ICurrencyService
{
    Task<ExchangeRate> GetLatestRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);

    Task<ConversionResult> ConvertCurrencyAsync(ConversionRequest request,
        CancellationToken cancellationToken = default);

    Task<PaginatedResult<ExchangeRate>> GetHistoricalRatesAsync(HistoricalRatesRequest request,
        CancellationToken cancellationToken = default);
}