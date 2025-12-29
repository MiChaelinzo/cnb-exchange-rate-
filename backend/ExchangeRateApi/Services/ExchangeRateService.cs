using ExchangeRateApi.Models;

namespace ExchangeRateApi.Services;

public interface IExchangeRateService
{
    Task<ExchangeRateResponse> GetExchangeRatesAsync();
    Task<ExchangeRateResponse> GetExchangeRatesAsync(DateTime date);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(IExchangeRateProvider exchangeRateProvider, ILogger<ExchangeRateService> logger)
    {
        _exchangeRateProvider = exchangeRateProvider;
        _logger = logger;
    }

    public async Task<ExchangeRateResponse> GetExchangeRatesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving current exchange rates from provider");
            var response = await _exchangeRateProvider.GetCurrentRatesAsync();
            
            // Apply business logic transformations if needed
            var processedResponse = ProcessExchangeRateResponse(response);
            
            _logger.LogInformation("Successfully processed {Count} exchange rates for date {Date}", 
                processedResponse.Rates.Count, processedResponse.Date);
            
            return processedResponse;
        }
        catch (CnbServiceException ex)
        {
            _logger.LogError(ex, "CNB service error occurred while fetching exchange rates");
            throw new CnbServiceUnavailableException("CNB service is currently unavailable", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching exchange rates");
            throw new CnbServiceUnavailableException("CNB service is currently unavailable due to network issues", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout occurred while fetching exchange rates");
            throw new CnbServiceUnavailableException("CNB service request timed out", ex);
        }
        catch (CnbDataParsingException ex)
        {
            _logger.LogError(ex, "Failed to parse CNB data");
            throw new CnbServiceUnavailableException("CNB service returned invalid data", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in ExchangeRateService");
            throw;
        }
    }

    public async Task<ExchangeRateResponse> GetExchangeRatesAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Retrieving exchange rates for date {Date} from provider", date);
            var response = await _exchangeRateProvider.GetRatesAsync(date);
            
            // Apply business logic transformations if needed
            var processedResponse = ProcessExchangeRateResponse(response);
            
            _logger.LogInformation("Successfully processed {Count} exchange rates for date {Date}", 
                processedResponse.Rates.Count, processedResponse.Date);
            
            return processedResponse;
        }
        catch (CnbServiceException ex)
        {
            _logger.LogError(ex, "CNB service error occurred while fetching exchange rates for date {Date}", date);
            throw new CnbServiceUnavailableException($"CNB service is currently unavailable for date {date:yyyy-MM-dd}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching exchange rates for date {Date}", date);
            throw new CnbServiceUnavailableException("CNB service is currently unavailable due to network issues", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout occurred while fetching exchange rates for date {Date}", date);
            throw new CnbServiceUnavailableException("CNB service request timed out", ex);
        }
        catch (CnbDataParsingException ex)
        {
            _logger.LogError(ex, "Failed to parse CNB data for date {Date}", date);
            throw new CnbServiceUnavailableException("CNB service returned invalid data", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in ExchangeRateService for date {Date}", date);
            throw;
        }
    }

    private ExchangeRateResponse ProcessExchangeRateResponse(ExchangeRateResponse response)
    {
        // Apply business logic transformations
        var processedRates = response.Rates
            .Where(rate => IsValidForDisplay(rate))
            .OrderBy(rate => rate.Code)
            .ToList();

        return new ExchangeRateResponse
        {
            Date = response.Date,
            SequenceNumber = response.SequenceNumber,
            Rates = processedRates
        };
    }

    private bool IsValidForDisplay(ExchangeRate rate)
    {
        // Business logic to determine if a rate should be displayed
        // For now, include all valid rates
        return !string.IsNullOrWhiteSpace(rate.Country) &&
               !string.IsNullOrWhiteSpace(rate.Currency) &&
               !string.IsNullOrWhiteSpace(rate.Code) &&
               rate.Code.Length == 3 &&
               rate.Amount > 0 &&
               rate.Rate > 0;
    }
}

public class CnbServiceUnavailableException : Exception
{
    public CnbServiceUnavailableException(string message) : base(message) { }
    public CnbServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}