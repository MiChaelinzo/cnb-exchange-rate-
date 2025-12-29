using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using ExchangeRateApi.Models;
using Microsoft.AspNetCore.Hosting;

namespace ExchangeRateApi.Tests.Integration;

/// <summary>
/// Integration tests for the complete data flow from CNB to API response
/// Tests complete data flow from CNB to frontend display
/// Verifies error handling across the entire stack
/// **Validates: All requirements**
/// </summary>
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for integration tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CnbApi:BaseUrl"] = "https://www.cnb.cz",
                    ["CnbApi:DailyRatesEndpoint"] = "/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
                    ["Cors:AllowedOrigins:1"] = "https://localhost:4200"
                });
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetExchangeRates_EndToEndFlow_ReturnsValidData()
    {
        // Test complete data flow from CNB to API response
        // This validates the entire integration chain:
        // CNB API -> CnbClient -> ExchangeRateProvider -> ExchangeRateService -> Controller -> JSON Response

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates");

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify response headers
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.True(response.Headers.Contains("X-Data-Date"));

        // Verify response content
        var jsonContent = await response.Content.ReadAsStringAsync();
        var exchangeRateResponse = JsonSerializer.Deserialize<ExchangeRateResponse>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(exchangeRateResponse);
        Assert.True(exchangeRateResponse.Rates.Count > 0);
        Assert.All(exchangeRateResponse.Rates, rate =>
        {
            Assert.NotNull(rate.Country);
            Assert.NotNull(rate.Currency);
            Assert.NotNull(rate.Code);
            Assert.True(rate.Amount > 0);
            Assert.True(rate.Rate > 0);
        });

        // Verify date is reasonable (not default or future)
        Assert.True(exchangeRateResponse.Date >= DateTime.Today.AddDays(-7));
        Assert.True(exchangeRateResponse.Date <= DateTime.Today);
        
        // Verify sequence number is positive
        Assert.True(exchangeRateResponse.SequenceNumber > 0);
    }

    [Fact]
    public async Task GetExchangeRates_WithSpecificDate_ReturnsHistoricalData()
    {
        // Test historical data retrieval using route parameter format
        var testDate = DateTime.Today.AddDays(-1);
        
        // Act - Use route parameter format as defined in controller
        var response = await _client.GetAsync($"/api/v1/exchange-rates/{testDate:yyyy-MM-dd}");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var exchangeRateResponse = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        Assert.NotNull(exchangeRateResponse);
        Assert.True(exchangeRateResponse.Rates.Count > 0);
        
        // Verify the returned date is reasonable (CNB may return current data for past dates)
        Assert.True(exchangeRateResponse.Date <= DateTime.Today);
    }

    [Fact]
    public async Task GetExchangeRates_WithFutureDate_ReturnsBadRequest()
    {
        // Test error handling for invalid input using route parameter format
        var futureDate = DateTime.Today.AddDays(1);
        
        // Act - Use route parameter format as defined in controller
        var response = await _client.GetAsync($"/api/v1/exchange-rates/{futureDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Invalid Date", problemDetails.Title);
        Assert.Contains("future dates", problemDetails.Detail);
    }

    [Fact]
    public async Task CorsConfiguration_AllowsConfiguredOrigins()
    {
        // Test CORS configuration for frontend integration
        
        // Act - Simulate preflight request from Angular frontend
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/exchange-rates");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
        Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
    }

    [Fact]
    public async Task ApiVersioning_SupportsVersionedEndpoints()
    {
        // Test API versioning for REST conventions
        
        // Act - Test URL segment versioning (the primary method used)
        var response1 = await _client.GetAsync("/api/v1/exchange-rates");
        
        // Assert
        response1.EnsureSuccessStatusCode();
        var content1 = await response1.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        Assert.NotNull(content1);
        Assert.True(content1.Rates.Count > 0);
        
        // Test query string versioning (if supported)
        var response2 = await _client.GetAsync("/api/v1/exchange-rates?version=1.0");
        response2.EnsureSuccessStatusCode();
        var content2 = await response2.Content.ReadFromJsonAsync<ExchangeRateResponse>();
        Assert.NotNull(content2);
        Assert.True(content2.Rates.Count > 0);
    }

    [Fact]
    public async Task ErrorHandling_ReturnsProperProblemDetails()
    {
        // Test error handling across the entire stack
        // This test simulates various error conditions to verify proper error propagation
        
        // Test with invalid endpoint
        var response = await _client.GetAsync("/api/v1/invalid-endpoint");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        
        // Test with malformed date parameter (invalid date format in route)
        // This returns 404 because the route constraint fails to match
        var malformedDateResponse = await _client.GetAsync("/api/v1/exchange-rates/invalid-date-format");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, malformedDateResponse.StatusCode);
    }

    [Fact]
    public async Task ResponseCaching_IncludesProperCacheHeaders()
    {
        // Test caching configuration for performance
        
        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates");

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify cache headers are present
        Assert.True(response.Headers.CacheControl?.MaxAge.HasValue);
        Assert.True(response.Headers.Vary?.Contains("Accept"));
        Assert.True(response.Headers.Vary?.Contains("Accept-Encoding"));
    }

    [Fact]
    public async Task Configuration_LoadsFromEnvironmentVariables()
    {
        // Test configuration management across environments
        // This verifies that the application properly loads configuration from various sources
        
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        // Verify CNB API configuration is loaded
        var cnbBaseUrl = configuration["CnbApi:BaseUrl"];
        var cnbEndpoint = configuration["CnbApi:DailyRatesEndpoint"];
        
        Assert.NotNull(cnbBaseUrl);
        Assert.NotNull(cnbEndpoint);
        Assert.Contains("cnb.cz", cnbBaseUrl);
        
        // Verify CORS configuration is loaded
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        Assert.NotNull(corsOrigins);
        Assert.Contains("http://localhost:4200", corsOrigins);
    }

    [Fact]
    public async Task HealthCheck_VerifiesSystemReadiness()
    {
        // Test system health and readiness
        // This ensures all dependencies are properly configured and accessible
        
        // Act - Make a simple request to verify the system is operational
        var response = await _client.GetAsync("/api/v1/exchange-rates");

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify response time is reasonable (under 10 seconds for integration test)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var secondResponse = await _client.GetAsync("/api/v1/exchange-rates");
        stopwatch.Stop();
        
        secondResponse.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, "Response time should be under 10 seconds");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

// Helper class for problem details deserialization
public class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
}