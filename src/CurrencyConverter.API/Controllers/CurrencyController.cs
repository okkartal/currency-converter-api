using CurrencyConverter.Core.Contracts;
using CurrencyConverter.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("rates")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency = "EUR",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rates = await _currencyService.GetLatestRatesAsync(baseCurrency, cancellationToken);
            return Ok(rates);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting latest rates for {baseCurrency}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occured while processing your request" });
        }
    }

    [HttpPost("convert")]
    [Authorize(Roles = "User,Admin")]
    public async Task<IActionResult> ConvertCurrency([FromBody] ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _currencyService.ConvertCurrencyAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error converting currency from {request?.FromCurrency}  to {request?.ToCurrency} {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occured while processing your request" });
        }
    }

    [HttpGet("historical")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetHistoricalRates(
        [FromQuery] string baseCurrency = "EUR",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            //default to last 30 days if dates not provided
            var now = DateTime.UtcNow;
            var start = startDate ?? now.AddDays(-30);
            var end = endDate ?? now;

            var request = new HistoricalRatesRequest(
                baseCurrency, start, end, page, pageSize);

            var response = await _currencyService.GetHistoricalRatesAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting historical rates for {baseCurrency}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occured while processing your request" });
        }
    }
}