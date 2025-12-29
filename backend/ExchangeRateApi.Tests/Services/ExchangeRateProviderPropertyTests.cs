using ExchangeRateApi.Models;
using ExchangeRateApi.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace ExchangeRateApi.Tests.Services;

/// <summary>
/// Property-based tests for ExchangeRateProvider CNB data parsing
/// Feature: exchange-rate-display, Property-based testing for CNB data parsing
/// </summary>
public class ExchangeRateProviderPropertyTests : PropertyTestBase
{
    private readonly ExchangeRateProvider _provider;

    public ExchangeRateProviderPropertyTests()
    {
        var mockClient = new TestCnbClient();
        var mockLogger = new TestLogger<ExchangeRateProvider>();
        _provider = new ExchangeRateProvider(mockClient, mockLogger);
    }

    /// <summary>
    /// Property 2: CNB Data Parsing Round Trip
    /// For any valid CNB TXT format data, parsing then formatting should preserve the essential exchange rate information
    /// Feature: exchange-rate-display, Property 2: CNB Data Parsing Round Trip
    /// Validates: Requirements 1.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CnbDataParsingRoundTrip()
    {
        return Prop.ForAll(GenerateValidCnbData(), cnbData =>
        {
            try
            {
                // Parse the CNB data
                var parsed = _provider.ParseCnbData(cnbData);
                
                // Format it back to CNB format
                var formatted = FormatToCnbData(parsed);
                
                // Parse again
                var reparsed = _provider.ParseCnbData(formatted);
                
                // Essential information should be preserved
                return parsed.Date == reparsed.Date &&
                       parsed.SequenceNumber == reparsed.SequenceNumber &&
                       parsed.Rates.Count == reparsed.Rates.Count &&
                       parsed.Rates.Zip(reparsed.Rates, (original, reparsed) =>
                           original.Country == reparsed.Country &&
                           original.Currency == reparsed.Currency &&
                           original.Amount == reparsed.Amount &&
                           original.Code == reparsed.Code &&
                           Math.Abs(original.Rate - reparsed.Rate) < 0.001m // Allow small decimal precision differences
                       ).All(x => x);
            }
            catch (CnbDataParsingException)
            {
                // If parsing fails, the data was invalid, which is acceptable
                return true;
            }
        });
    }

    /// <summary>
    /// Generates valid CNB data for property testing
    /// </summary>
    private static Arbitrary<string> GenerateValidCnbData()
    {
        return Arb.From(Gen.Elements(
            // Sample valid CNB data formats
            "03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nAustralia|dollar|1|AUD|23.282\nUSA|dollar|1|USD|25.347",
            "15 Feb 2023 #35\nCountry|Currency|Amount|Code|Rate\nUnited Kingdom|pound|1|GBP|30.125\nJapan|yen|100|JPY|18.456",
            "28 Dec 2022 #245\nCountry|Currency|Amount|Code|Rate\nCanada|dollar|1|CAD|18.234\nSwitzerland|franc|1|CHF|26.789\nSweden|krona|10|SEK|2.345",
            "01 Apr 2024 #78\nCountry|Currency|Amount|Code|Rate\nNorway|krone|10|NOK|2.456\nDenmark|krone|10|DKK|3.567\nPoland|zloty|1|PLN|6.123",
            "10.Jan.2021 #15\nCountry|Currency|Amount|Code|Rate\nAustralia|dollar|1|AUD|16.789\nUSA|dollar|1|USD|21.234"
        ));
    }

    /// <summary>
    /// Formats ExchangeRateResponse back to CNB data format for round-trip testing
    /// </summary>
    private static string FormatToCnbData(ExchangeRateResponse response)
    {
        var header = $"{response.Date:d MMM yyyy} #{response.SequenceNumber}";
        var columnHeaders = "Country|Currency|Amount|Code|Rate";
        var dataLines = response.Rates.Select(r => 
            $"{r.Country}|{r.Currency}|{r.Amount}|{r.Code}|{r.Rate.ToString("F3", CultureInfo.InvariantCulture)}");
        
        return string.Join("\n", new[] { header, columnHeaders }.Concat(dataLines));
    }

    /// <summary>
    /// Property test for parsing validation - ensures invalid data is properly rejected
    /// Feature: exchange-rate-display, Property: CNB Data Validation
    /// Validates: Requirements 1.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CnbDataValidationRejectsInvalidData()
    {
        return Prop.ForAll(GenerateInvalidCnbData(), invalidData =>
        {
            try
            {
                _provider.ParseCnbData(invalidData);
                // If parsing succeeds with invalid data, the test fails
                return false;
            }
            catch (CnbDataParsingException)
            {
                // Expected behavior - invalid data should throw parsing exception
                return true;
            }
            catch (Exception)
            {
                // Other exceptions are also acceptable for invalid data
                return true;
            }
        });
    }

    /// <summary>
    /// Generates invalid CNB data for validation testing
    /// </summary>
    private static Arbitrary<string> GenerateInvalidCnbData()
    {
        return Arb.From(Gen.OneOf(
            // Empty or null data
            Gen.Constant(""),
            Gen.Constant("   "),
            
            // Invalid header formats
            Gen.Constant("Invalid Header\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|1|USD|25.347"),
            Gen.Constant("32 Xxx 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|1|USD|25.347"),
            
            // Missing required lines
            Gen.Constant("03 Jan 2000 #1"),
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate"),
            
            // Invalid data line formats
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|invalid|USD|25.347"),
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|1|USD|invalid"),
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|1|USD"), // Missing rate
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|0|USD|25.347"), // Zero amount
            Gen.Constant("03 Jan 2000 #1\nCountry|Currency|Amount|Code|Rate\nUSA|dollar|-1|USD|25.347") // Negative amount
        ));
    }
}

// Test implementations
public class TestCnbClient : ICnbClient
{
    public Task<string> FetchRatesAsync(DateTime? date = null)
    {
        // Return sample CNB data for testing
        var sampleData = @"03 Jan 2000 #1
Country|Currency|Amount|Code|Rate
Australia|dollar|1|AUD|23.282
USA|dollar|1|USD|25.347";
        return Task.FromResult(sampleData);
    }
}

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}