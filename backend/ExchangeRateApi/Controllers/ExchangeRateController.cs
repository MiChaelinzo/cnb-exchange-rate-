using Microsoft.AspNetCore.Mvc;
using ExchangeRateApi.Models;
using ExchangeRateApi.Services;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;

namespace ExchangeRateApi.Controllers;

/// <summary>
/// Controller for managing exchange rate operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exchange-rates")]
[Produces("application/json")]
[Consumes("application/json")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateController> _logger;

    public ExchangeRateController(IExchangeRateService exchangeRateService, ILogger<ExchangeRateController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current exchange rates from CNB
    /// </summary>
    /// <returns>Current exchange rates with date and sequence information</returns>
    /// <response code="200">Returns current exchange rates</response>
    /// <response code="503">CNB service is unavailable</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ResponseCache(CacheProfileName = "Default")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRates()
    {
        try
        {
            _logger.LogInformation("Received request for current exchange rates");
            
            var rates = await _exchangeRateService.GetExchangeRatesAsync();
            
            _logger.LogInformation("Successfully retrieved {Count} exchange rates for date {Date}", 
                rates.Rates.Count, rates.Date);
            
            // Add response headers for REST compliance
            Response.Headers["X-Total-Count"] = rates.Rates.Count.ToString();
            Response.Headers["X-Data-Date"] = rates.Date.ToString("yyyy-MM-dd");
            
            return Ok(rates);
        }
        catch (CnbServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "CNB service is unavailable");
            return Problem(
                title: "CNB Service Unavailable",
                detail: "The Czech National Bank service is currently unavailable. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.4"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving exchange rates");
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while processing your request.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            );
        }
    }

    /// <summary>
    /// Gets exchange rates for a specific date
    /// </summary>
    /// <param name="date">Date for which to retrieve exchange rates (YYYY-MM-DD format)</param>
    /// <returns>Exchange rates for the specified date</returns>
    /// <response code="200">Returns exchange rates for the specified date</response>
    /// <response code="400">Invalid date format</response>
    /// <response code="404">Exchange rates not found for the specified date</response>
    /// <response code="503">CNB service is unavailable</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("{date:datetime}")]
    [ResponseCache(CacheProfileName = "Default")]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRates([Required] DateTime date)
    {
        try
        {
            _logger.LogInformation("Received request for exchange rates for date {Date}", date);
            
            // Validate date is not in the future
            if (date.Date > DateTime.Today)
            {
                _logger.LogWarning("Request for future date {Date} rejected", date);
                return Problem(
                    title: "Invalid Date",
                    detail: "Exchange rates are not available for future dates.",
                    statusCode: StatusCodes.Status400BadRequest,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                );
            }
            
            var rates = await _exchangeRateService.GetExchangeRatesAsync(date);
            
            _logger.LogInformation("Successfully retrieved {Count} exchange rates for date {Date}", 
                rates.Rates.Count, rates.Date);
            
            // Add response headers for REST compliance
            Response.Headers["X-Total-Count"] = rates.Rates.Count.ToString();
            Response.Headers["X-Data-Date"] = rates.Date.ToString("yyyy-MM-dd");
            
            return Ok(rates);
        }
        catch (CnbServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "CNB service is unavailable for date {Date}", date);
            return Problem(
                title: "CNB Service Unavailable",
                detail: $"The Czech National Bank service is currently unavailable for date {date:yyyy-MM-dd}. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.4"
            );
        }
        catch (CnbDataNotFoundException ex)
        {
            _logger.LogWarning(ex, "Exchange rates not found for date {Date}", date);
            return Problem(
                title: "Data Not Found",
                detail: $"Exchange rates are not available for date {date:yyyy-MM-dd}.",
                statusCode: StatusCodes.Status404NotFound,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving exchange rates for date {Date}", date);
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred while processing your request.",
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            );
        }
    }
}