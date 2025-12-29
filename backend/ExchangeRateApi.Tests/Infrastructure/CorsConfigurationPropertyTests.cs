using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace ExchangeRateApi.Tests.Infrastructure;

/// <summary>
/// Feature: exchange-rate-display, Property 5: CORS Configuration
/// Tests that cross-origin requests from Angular frontend include appropriate CORS headers
/// **Validates: Requirements 2.5**
/// </summary>
public class CorsConfigurationPropertyTests : PropertyTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsConfigurationPropertyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Property(MaxTest = 100)]
    public Property CorsHeaders_ShouldBePresent_ForAnyOriginRequest()
    {
        return Prop.ForAll(GenerateValidOrigins(), origin =>
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Cors:AllowedOrigins:0"] = origin,
                        ["CnbApi:BaseUrl"] = "https://www.cnb.cz",
                        ["CnbApi:DailyRatesEndpoint"] = "/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt"
                    });
                });
            }).CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/exchange-rates");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

            var response = client.SendAsync(request).Result;

            // Assert - Property 5: CORS Configuration
            var corsHeaders = response.Headers.Where(h => h.Key.StartsWith("Access-Control-")).ToList();
            
            return corsHeaders.Any(h => h.Key == "Access-Control-Allow-Origin") &&
                   corsHeaders.Any(h => h.Key == "Access-Control-Allow-Methods") &&
                   corsHeaders.Any(h => h.Key == "Access-Control-Allow-Headers");
        });
    }

    [Property(MaxTest = 100)]
    public Property CorsHeaders_ShouldAllowConfiguredOrigins_ForAnyValidOrigin()
    {
        return Prop.ForAll(GenerateValidOrigins(), origin =>
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Cors:AllowedOrigins:0"] = origin,
                        ["CnbApi:BaseUrl"] = "https://www.cnb.cz",
                        ["CnbApi:DailyRatesEndpoint"] = "/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt"
                    });
                });
            }).CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/exchange-rates");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");

            var response = client.SendAsync(request).Result;

            // Assert - Property 5: CORS Configuration
            var allowOriginHeader = response.Headers
                .FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin");

            return allowOriginHeader.Key != null && 
                   (allowOriginHeader.Value.Contains(origin) || allowOriginHeader.Value.Contains("*"));
        });
    }

    [Property(MaxTest = 100)]
    public Property CorsHeaders_ShouldAllowRequiredMethods_ForAnyRequest()
    {
        return Prop.ForAll(GenerateHttpMethods(), method =>
        {
            // Arrange
            var origin = "http://localhost:4200";
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Cors:AllowedOrigins:0"] = origin,
                        ["CnbApi:BaseUrl"] = "https://www.cnb.cz",
                        ["CnbApi:DailyRatesEndpoint"] = "/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt"
                    });
                });
            }).CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/exchange-rates");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", method);

            var response = client.SendAsync(request).Result;

            // Assert - Property 5: CORS Configuration
            var allowMethodsHeader = response.Headers
                .FirstOrDefault(h => h.Key == "Access-Control-Allow-Methods");

            return allowMethodsHeader.Key != null && 
                   (allowMethodsHeader.Value.Any(v => v.Contains(method)) || 
                    allowMethodsHeader.Value.Any(v => v.Contains("*")));
        });
    }

    /// <summary>
    /// Generates valid origin URLs for property testing
    /// </summary>
    private static Arbitrary<string> GenerateValidOrigins()
    {
        return Arb.From(Gen.Elements(
            "http://localhost:4200",
            "http://localhost:3000", 
            "https://example.com",
            "https://test.local",
            "http://127.0.0.1:4200",
            "https://app.example.com"
        ));
    }

    /// <summary>
    /// Generates HTTP methods for property testing
    /// </summary>
    private static Arbitrary<string> GenerateHttpMethods()
    {
        return Arb.From(Gen.Elements("GET", "POST", "PUT", "DELETE", "OPTIONS"));
    }

    [Fact]
    public async Task CorsConfiguration_ShouldBeProperlyConfigured_ForAngularFrontend()
    {
        // Unit test to verify basic CORS configuration for Angular frontend
        var origin = "http://localhost:4200";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = origin,
                    ["CnbApi:BaseUrl"] = "https://www.cnb.cz",
                    ["CnbApi:DailyRatesEndpoint"] = "/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt"
                });
            });
        }).CreateClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/exchange-rates");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var corsHeaders = response.Headers.Where(h => h.Key.StartsWith("Access-Control-")).ToList();
        Assert.Contains(corsHeaders, h => h.Key == "Access-Control-Allow-Origin");
        Assert.Contains(corsHeaders, h => h.Key == "Access-Control-Allow-Methods");
        Assert.Contains(corsHeaders, h => h.Key == "Access-Control-Allow-Headers");
    }
}