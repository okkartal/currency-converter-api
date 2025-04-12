using CurrencyConverter.Infrastructure.Services;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.Tests;

public sealed class CurrencyServiceTest
{
    private readonly Mock<ICurrencyProvider> _mockCurrencyProvider;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly CurrencyService _currencyService;
    
    public CurrencyServiceTest()
    {
        Mock<ICurrencyProviderFactory> mockCurrencyProviderFactory = new Mock<ICurrencyProviderFactory>();
        _mockCurrencyProvider = new Mock<ICurrencyProvider>();
        _mockCacheService = new Mock<ICacheService>();
        Mock<ILogger<CurrencyService>> mockLogger = new Mock<ILogger<CurrencyService>>();
        
        _currencyService = new CurrencyService(
            mockCurrencyProviderFactory.Object,
            _mockCacheService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task GetLatestRatesAsync_WithValidBaseCurrency_ReturnsRates()
    {
        //Arrange
        var baseCurrency = "EUR";

        var expectedRates = new ExchangeRate
        {
            Base = baseCurrency,
            Date = DateTime.Today,
            Rates = new Dictionary<string, decimal>
            {
                {"USD", 1.1m },
                {"GBP", 0.9m }
            }
        };
        
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ExchangeRate>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRates);
        
        //Act
        var result = await _currencyService.GetLatestRatesAsync(baseCurrency);
        
        //Assert
        Assert.Equal(baseCurrency, result.Base);
        Assert.Equal(expectedRates.Date, result.Date);
        Assert.Equal(expectedRates.Rates.Count, result.Rates.Count);
        Assert.Equal(expectedRates.Rates["USD"], result.Rates["USD"]);
        Assert.Equal(expectedRates.Rates["GBP"], result.Rates["GBP"]);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task GetLatestRatesAsync_WithRestrictedCurrency_ThrowsInvalidOperationException(string baseCurrency)
    {
        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await _currencyService.GetLatestRatesAsync(baseCurrency));
    }

    [Fact]
    public async Task ConvertCurrencyAsync_WithValidRequest_ReturnsConversionResult()
    {
        //Arrange
        var request = new ConversionRequest("EUR", "USD", 100m);

        var exchangeRate = new ExchangeRate
        {
            Base = "EUR",
            Date = DateTime.Today,
            Rates = new Dictionary<string, decimal>
            {
                { "USD", 1.1m },
            }
        };
        
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ExchangeRate>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exchangeRate);
        
        //Act
        var result = await _currencyService.ConvertCurrencyAsync(request);
        
        //Assert
        Assert.Equal(request.FromCurrency, result.FromCurrency);
        Assert.Equal(request.ToCurrency, result.ToCurrency);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(110m, result.ConvertedAmount);
        Assert.Equal(1.1m, result.Rate);
        Assert.Equal(exchangeRate.Date, result.Date);
    }


    [Fact]
    public async Task GetHistoricalRatesAsync_WithValidRequest_ReturnsPaginatedResult()
    {
        //Arrange
        var request = new HistoricalRatesRequest("EUR",
            new DateTime(2020, 1, 1),
            new DateTime(2020, 1, 10),
            1,
            5);

        var historicalRates = Enumerable.Range(0, 10).Select(i => new ExchangeRate
        {
            Base = "EUR",
            Date = new DateTime(2020, 1, 1).AddDays(i),
            Rates = new Dictionary<string, decimal>
            {
                { "USD", 1.1m + (i + 0.01m) },
            }
        }).ToList();
        
        _mockCurrencyProvider
            .Setup(p => p.GetHistoricalRatesAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalRates);

        var paginatedResult = new PaginatedResult<ExchangeRate>
        {
            Items = historicalRates.OrderByDescending(r => r.Date).Take(5).ToList(),
            PageNumber = 1,
            PageSize = 5,
            TotalCount = 10
        };
        
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PaginatedResult<ExchangeRate>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        
        //Act
        var result = await _currencyService.GetHistoricalRatesAsync(request);
        
        //Assert
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(10, result.TotalCount);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}