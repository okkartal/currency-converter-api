namespace CurrencyConverter.Core.Models;

public sealed record ConversionRequest(
    string FromCurrency,
    string ToCurrency,
    decimal Amount);