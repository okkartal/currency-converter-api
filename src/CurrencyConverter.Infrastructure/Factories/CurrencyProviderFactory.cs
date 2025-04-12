using CurrencyConverter.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure.Factories;

public class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly ILogger<CurrencyProviderFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CurrencyProviderFactory(IServiceProvider serviceProvider, ILogger<CurrencyProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public ICurrencyProvider GetProvider(string? providerName = null)
    {
        if (string.IsNullOrEmpty(providerName))
        {
            _logger.LogInformation($"No specified provider name for {providerName}");
            return _serviceProvider.GetRequiredService<ICurrencyProvider>();
        }

        //Get all registered providers
        var providers = _serviceProvider.GetServices<ICurrencyProvider>();

        //Find provider by name
        var provider =
            providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            _logger.LogWarning($"Provider not found for {providerName}");
            return _serviceProvider.GetRequiredService<ICurrencyProvider>();
        }

        _logger.LogInformation($"Provider found for {providerName}");
        return provider;
    }
}