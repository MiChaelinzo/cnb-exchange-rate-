using ExchangeRateApi.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExchangeRateApi.Services;

public interface IExchangeRateProvider
{
    Task<ExchangeRateResponse> GetCurrentRatesAsync();
    Task<ExchangeRateResponse> GetRatesAsync(DateTime date);
}

public class ExchangeRateProvider : IExchangeRateProvider
{
    private readonly ICnbClient _cnbClient;
    private readonly ILogger<ExchangeRateProvider> _logger;

    public ExchangeRateProvider(ICnbClient cnbClient, ILogger<ExchangeRateProvider> logger)
    {
        _cnbClient = cnbClient;
        _logger = logger;
    }

    public async Task<ExchangeRateResponse> GetCurrentRatesAsync()
    {
        try
        {
            _logger.LogInformation("Attempting to fetch current exchange rates from CNB");
            var data = await _cnbClient.FetchRatesAsync();
            
            if (string.IsNullOrWhiteSpace(data))
            {
                _logger.LogError("CNB API returned empty or null data");
                throw new CnbServiceException("CNB API returned empty response");
            }
            
            var result = ParseCnbData(data);
            _logger.LogInformation("Successfully retrieved {Count} exchange rates for date {Date}", 
                result.Rates.Count, result.Date);
            return result;
        }
        catch (CnbApiException ex)
        {
            _logger.LogError(ex, "CNB API is unavailable: {Message}", ex.Message);
            throw new CnbServiceException("CNB service is currently unavailable", ex);
        }
        catch (CnbDataNotFoundException ex)
        {
            _logger.LogError(ex, "CNB data not found: {Message}", ex.Message);
            throw new CnbServiceException("Requested exchange rate data is not available", ex);
        }
        catch (CnbDataParsingException ex)
        {
            _logger.LogError(ex, "Failed to parse CNB data: {Message}", ex.Message);
            throw new CnbServiceException("CNB service returned invalid data format", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while connecting to CNB API");
            throw new CnbServiceException("Network error occurred while accessing CNB service", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request to CNB API timed out");
            throw new CnbServiceException("CNB service request timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching current exchange rates");
            throw new CnbServiceException("An unexpected error occurred while retrieving exchange rates", ex);
        }
    }

    public async Task<ExchangeRateResponse> GetRatesAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Attempting to fetch exchange rates from CNB for date {Date}", date);
            var data = await _cnbClient.FetchRatesAsync(date);
            
            if (string.IsNullOrWhiteSpace(data))
            {
                _logger.LogError("CNB API returned empty or null data for date {Date}", date);
                throw new CnbServiceException($"CNB API returned empty response for date {date:yyyy-MM-dd}");
            }
            
            var result = ParseCnbData(data);
            _logger.LogInformation("Successfully retrieved {Count} exchange rates for date {Date}", 
                result.Rates.Count, result.Date);
            return result;
        }
        catch (CnbApiException ex)
        {
            _logger.LogError(ex, "CNB API is unavailable for date {Date}: {Message}", date, ex.Message);
            throw new CnbServiceException($"CNB service is currently unavailable for date {date:yyyy-MM-dd}", ex);
        }
        catch (CnbDataNotFoundException ex)
        {
            _logger.LogError(ex, "CNB data not found for date {Date}: {Message}", date, ex.Message);
            throw new CnbServiceException($"Exchange rate data is not available for date {date:yyyy-MM-dd}", ex);
        }
        catch (CnbDataParsingException ex)
        {
            _logger.LogError(ex, "Failed to parse CNB data for date {Date}: {Message}", date, ex.Message);
            throw new CnbServiceException($"CNB service returned invalid data format for date {date:yyyy-MM-dd}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while connecting to CNB API for date {Date}", date);
            throw new CnbServiceException($"Network error occurred while accessing CNB service for date {date:yyyy-MM-dd}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request to CNB API timed out for date {Date}", date);
            throw new CnbServiceException($"CNB service request timed out for date {date:yyyy-MM-dd}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching exchange rates for date {Date}", date);
            throw new CnbServiceException($"An unexpected error occurred while retrieving exchange rates for date {date:yyyy-MM-dd}", ex);
        }
    }

    public ExchangeRateResponse ParseCnbData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            _logger.LogError("CNB data is null, empty, or whitespace");
            throw new CnbDataParsingException("CNB data cannot be null or empty");
        }

        try
        {
            var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2)
            {
                _logger.LogError("CNB data has insufficient lines. Expected at least 2, got {LineCount}", lines.Length);
                throw new CnbDataParsingException("CNB data must contain at least header and one data line");
            }

            // Parse header line: "03 Jan 2000 #1"
            var headerLine = lines[0].Trim();
            _logger.LogDebug("Parsing CNB header: {Header}", headerLine);
            var (date, sequenceNumber) = ParseHeader(headerLine);

            // Skip the column headers line (Country|Currency|Amount|Code|Rate)
            if (lines.Length < 3)
            {
                _logger.LogError("CNB data missing column headers or data lines. Total lines: {LineCount}", lines.Length);
                throw new CnbDataParsingException("CNB data must contain header, column headers, and at least one data line");
            }

            var rates = new List<ExchangeRate>();
            var parseErrors = new List<string>();
            
            // Parse data lines starting from line 2 (index 2)
            for (int i = 2; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                {
                    _logger.LogDebug("Skipping empty line at index {Index}", i);
                    continue;
                }

                try
                {
                    var rate = ParseDataLine(line);
                    if (IsValidExchangeRate(rate))
                    {
                        rates.Add(rate);
                        _logger.LogDebug("Successfully parsed exchange rate: {Code} = {Rate}", rate.Code, rate.Rate);
                    }
                    else
                    {
                        var errorMsg = $"Invalid exchange rate data at line {i + 1}: {line}";
                        _logger.LogWarning(errorMsg);
                        parseErrors.Add(errorMsg);
                    }
                }
                catch (CnbDataParsingException ex)
                {
                    var errorMsg = $"Failed to parse line {i + 1}: {line} - {ex.Message}";
                    _logger.LogWarning(ex, errorMsg);
                    parseErrors.Add(errorMsg);
                    // Continue parsing other lines instead of failing completely
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Unexpected error parsing line {i + 1}: {line} - {ex.Message}";
                    _logger.LogWarning(ex, errorMsg);
                    parseErrors.Add(errorMsg);
                }
            }

            if (rates.Count == 0)
            {
                var errorMsg = $"No valid exchange rates found in CNB data. Parse errors: {string.Join("; ", parseErrors)}";
                _logger.LogError(errorMsg);
                throw new CnbDataParsingException(errorMsg);
            }

            if (parseErrors.Count > 0)
            {
                _logger.LogWarning("Successfully parsed {ValidCount} rates, but encountered {ErrorCount} parsing errors", 
                    rates.Count, parseErrors.Count);
            }

            _logger.LogInformation("Successfully parsed CNB data: {RateCount} exchange rates for date {Date} (sequence #{SequenceNumber})", 
                rates.Count, date, sequenceNumber);

            return new ExchangeRateResponse
            {
                Date = date,
                SequenceNumber = sequenceNumber,
                Rates = rates
            };
        }
        catch (CnbDataParsingException)
        {
            // Re-throw parsing exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while parsing CNB data");
            throw new CnbDataParsingException("Unexpected error occurred while parsing CNB data", ex);
        }
    }

    private (DateTime date, int sequenceNumber) ParseHeader(string headerLine)
    {
        // Expected format: "03 Jan 2000 #1" or "03.Jan.2000 #1"
        var headerRegex = new Regex(@"^(\d{1,2}[\s\.]?\w{3}[\s\.]?\d{4})\s*#(\d+)$", RegexOptions.IgnoreCase);
        var match = headerRegex.Match(headerLine);
        
        if (!match.Success)
        {
            throw new CnbDataParsingException($"Invalid header format: {headerLine}");
        }

        var dateString = match.Groups[1].Value;
        var sequenceString = match.Groups[2].Value;

        // Parse date - try multiple formats
        var dateFormats = new[]
        {
            "d MMM yyyy",
            "dd MMM yyyy", 
            "d.MMM.yyyy",
            "dd.MMM.yyyy"
        };

        DateTime date = default;
        var dateParsed = false;
        
        foreach (var format in dateFormats)
        {
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                dateParsed = true;
                break;
            }
        }

        if (!dateParsed)
        {
            throw new CnbDataParsingException($"Unable to parse date: {dateString}");
        }

        if (!int.TryParse(sequenceString, out var sequenceNumber) || sequenceNumber <= 0)
        {
            throw new CnbDataParsingException($"Invalid sequence number: {sequenceString}");
        }

        return (date, sequenceNumber);
    }

    private ExchangeRate ParseDataLine(string line)
    {
        // Expected format: "Australia|dollar|1|AUD|23.282"
        var parts = line.Split('|');
        
        if (parts.Length != 5)
        {
            throw new CnbDataParsingException($"Data line must have exactly 5 parts separated by '|': {line}");
        }

        var country = parts[0].Trim();
        var currency = parts[1].Trim();
        var amountString = parts[2].Trim();
        var code = parts[3].Trim();
        var rateString = parts[4].Trim();

        if (!int.TryParse(amountString, out var amount) || amount <= 0)
        {
            throw new CnbDataParsingException($"Invalid amount: {amountString}");
        }

        if (!decimal.TryParse(rateString, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
        {
            throw new CnbDataParsingException($"Invalid rate: {rateString}");
        }

        return new ExchangeRate
        {
            Country = country,
            Currency = currency,
            Amount = amount,
            Code = code,
            Rate = rate
        };
    }

    private bool IsValidExchangeRate(ExchangeRate rate)
    {
        return !string.IsNullOrWhiteSpace(rate.Country) &&
               !string.IsNullOrWhiteSpace(rate.Currency) &&
               !string.IsNullOrWhiteSpace(rate.Code) &&
               rate.Code.Length == 3 &&
               rate.Amount > 0 &&
               rate.Rate > 0;
    }
}

public class CnbDataParsingException : Exception
{
    public CnbDataParsingException(string message) : base(message) { }
    public CnbDataParsingException(string message, Exception innerException) : base(message, innerException) { }
}

public class CnbServiceException : Exception
{
    public CnbServiceException(string message) : base(message) { }
    public CnbServiceException(string message, Exception innerException) : base(message, innerException) { }
}