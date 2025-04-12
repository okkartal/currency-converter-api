using CurrencyConverter.API.Controllers;
using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.Tests;

public sealed class CurrencyControllerTest
{
    private readonly Mock<ICurrencyService> _mockCurrencyService;
    private readonly CurrencyController _controller;

    public CurrencyControllerTest()
    {
        _mockCurrencyService = new Mock<ICurrencyService>();
        Mock<ILogger<CurrencyController>> mockLogger = new Mock<ILogger<CurrencyController>>();
        
        _controller = new CurrencyController(_mockCurrencyService.Object, mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetLatestRates_ReturnsOkWithRates()
    {
        //Arrange
        var baseCurrency = "EUR";
        var expectedRates = new ExchangeRate
        {
            Base = baseCurrency,
            Date = DateTime.Today,
            Rates = new Dictionary<string, decimal>
            {
                { "USD", 1.1m },
                { "EUR", 0.9m }
            }
        };
        
        _mockCurrencyService
            .Setup(s => s.GetLatestRatesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRates);
        
        //Act
        var result = await _controller.GetLatestRates(baseCurrency);
        
        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRates = Assert.IsType<ExchangeRate>(okResult.Value);
        
        Assert.Equal(baseCurrency, returnedRates.Base);
        Assert.Equal(expectedRates.Date, returnedRates.Date);
        Assert.Equal(expectedRates.Rates.Count, returnedRates.Rates.Count);
    }

    [Fact]
    public async Task GetLatestRates_WithInvalidCurrency_ReturnsBadRequest()
    {
        //Arrange
        var baseCurrency = "TRY";

        _mockCurrencyService
            .Setup(s => s.GetLatestRatesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Currency TRY is restricted by policy"));
        
        //Act
        var result = await _controller.GetLatestRates(baseCurrency);
        
        //Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("restricted", badRequestResult?.Value?.ToString());
    }

    [Fact]
    public async Task ConvertCurrency_ReturnsOkWithConversionResult()
    {
        //Arrange
        var request = new ConversionRequest("EUR", "USD", 100m);
        
        var expectedResult = new ConversionResult("EUR", "USD", 100m,
            110m, 1.1m, DateTime.Today);
        
        _mockCurrencyService
            .Setup(s => s.ConvertCurrencyAsync(
                It.IsAny<ConversionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        //Act
        var result = await _controller.ConvertCurrency(request);
        
        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var conversionResult = Assert.IsType<ConversionResult>(okResult.Value);
        
        Assert.Equal(request.FromCurrency, conversionResult.FromCurrency);
        Assert.Equal(request.ToCurrency, conversionResult.ToCurrency);
        Assert.Equal(request.Amount, conversionResult.Amount);
        Assert.Equal(expectedResult.ConvertedAmount, conversionResult.ConvertedAmount);
        Assert.Equal(expectedResult.Rate, conversionResult.Rate);
    }

    [Fact]
    public async Task GetHistoricalRates_ReturnsOkWithPaginatedResult()
    {
        //Arrange
        var baseCurrency = "EUR";
        var startDate = new DateTime(2020, 1, 1);
        var endDate = new DateTime(2020, 1, 10);
        int page = 1,pageSize = 5;

        var historicalRates = Enumerable.Range(0, 5).Select(i => new ExchangeRate
        {
            Base = "EUR",
            Date = new DateTime(2020, 1, 10).AddDays(i),
            Rates = new Dictionary<string, decimal>
            {
                { "USD", 1.1m + (i * 0.01m) },
            }
        });

        var expectedResult = new PaginatedResult<ExchangeRate>
        {
            Items = historicalRates,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = 10
        };
        
        _mockCurrencyService
            .Setup(s => s.GetHistoricalRatesAsync(
                It.Is<HistoricalRatesRequest>(r =>
                    r.BaseCurrency == baseCurrency && 
                    r.StartDate == startDate && 
                    r.EndDate == endDate && 
                    r.Page == page &&
                    r.PageSize == pageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        
        //Act
        var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);
        
        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var paginatedResult = Assert.IsType<PaginatedResult<ExchangeRate>>(okResult.Value);
        
        Assert.Equal(expectedResult.Items.Count(), paginatedResult.Items.Count());
        Assert.Equal(expectedResult.PageNumber, paginatedResult.PageNumber);
        Assert.Equal(expectedResult.PageSize, paginatedResult.PageSize);
        Assert.Equal(expectedResult.TotalCount, paginatedResult.TotalCount);
    }
}