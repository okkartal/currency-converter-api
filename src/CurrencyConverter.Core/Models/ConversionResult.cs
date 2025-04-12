namespace CurrencyConverter.Core.Models;

public sealed record ConversionResult(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal ConvertedAmount,
    decimal Rate,
    DateTime Date);