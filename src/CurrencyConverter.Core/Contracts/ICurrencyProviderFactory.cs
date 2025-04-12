namespace CurrencyConverter.Core.Contracts;

public interface ICurrencyProviderFactory
{
    ICurrencyProvider GetProvider(string? providerName = null);
}