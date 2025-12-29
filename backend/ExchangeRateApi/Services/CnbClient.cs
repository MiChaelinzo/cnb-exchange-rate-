using Microsoft.Extensions.Options;
using System.Net;

namespace ExchangeRateApi.Services;

public interface ICnbClient
{
    Task<string> FetchRatesAsync(DateTime? date = null);
}

public class CnbClient : ICnbClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CnbClient> _logger;
    private readonly CnbClientOptions _options;

    public CnbClient(HttpClient httpClient, ILogger<CnbClient> logger, IOptions<CnbClientOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        
        // Configure timeout and retry policies
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<string> FetchRatesAsync(DateTime? date = null)
    {
        var url = date.HasValue 
            ? $"{_options.BaseUrl}{_options.DailyRatesEndpoint}?date={date.Value:dd.MM.yyyy}"
            : $"{_options.BaseUrl}{_options.DailyRatesEndpoint}";

        var retryCount = 0;
        var maxRetries = _options.MaxRetries;

        while (retryCount <= maxRetries)
        {
            try
            {
                _logger.LogInformation("Fetching CNB rates from {Url}, attempt {Attempt}", url, retryCount + 1);
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully fetched CNB rates");
                    return content;
                }
                
                if (response.StatusCode == HttpStatusCode.NotFound && date.HasValue)
                {
                    _logger.LogWarning("CNB rates not found for date {Date}", date.Value);
                    throw new CnbDataNotFoundException($"Exchange rates not found for date {date.Value:dd.MM.yyyy}");
                }
                
                _logger.LogWarning("CNB API returned {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                
                if (retryCount == maxRetries)
                {
                    throw new CnbApiException($"CNB API returned {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("Request to CNB API timed out, attempt {Attempt}", retryCount + 1);
                
                if (retryCount == maxRetries)
                {
                    throw new CnbApiException("CNB API request timed out after multiple attempts", ex);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP request failed, attempt {Attempt}", retryCount + 1);
                
                if (retryCount == maxRetries)
                {
                    throw new CnbApiException("Failed to connect to CNB API", ex);
                }
            }
            catch (CnbDataNotFoundException)
            {
                // Don't retry for not found errors
                throw;
            }
            catch (CnbApiException)
            {
                // Don't retry for API errors that are already wrapped
                throw;
            }

            retryCount++;
            if (retryCount <= maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs * Math.Pow(2, retryCount - 1));
                _logger.LogInformation("Retrying in {Delay}ms", delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
        }

        throw new CnbApiException("Unexpected error in retry logic");
    }
}

public class CnbClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string DailyRatesEndpoint { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class CnbApiException : Exception
{
    public CnbApiException(string message) : base(message) { }
    public CnbApiException(string message, Exception innerException) : base(message, innerException) { }
}

public class CnbDataNotFoundException : Exception
{
    public CnbDataNotFoundException(string message) : base(message) { }
}