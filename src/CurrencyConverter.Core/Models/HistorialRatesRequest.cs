namespace CurrencyConverter.Core.Models;

public sealed record HistoricalRatesRequest(
    string BaseCurrency,
    DateTime StartDate,
    DateTime EndDate,
    int Page = 1,
    int PageSize = 10);