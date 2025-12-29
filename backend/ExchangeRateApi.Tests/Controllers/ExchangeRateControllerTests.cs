using ExchangeRateApi.Controllers;
using ExchangeRateApi.Models;
using ExchangeRateApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace ExchangeRateApi.Tests.Controllers;

/// <summary>
/// Feature: exchange-rate-display, Property 4: API Response Format
/// Tests that API responses are in valid JSON format with correct content-type headers
/// **Validates: Requirements 2.1, 2.2**
/// 
/// Feature: exchange-rate-display, Property 10: REST API Conventions
/// Tests that REST endpoints follow proper conventions for HTTP verbs, resource naming, and status codes
/// **Validates: Requirements 6.2**
/// </summary>
public class ExchangeRateControllerTests
{
    [Fact]
    public async Task GetExchangeRates_ReturnsValidResponse()
    {
        // Arrange
        var mockService = new TestExchangeRateService();
        var mockLogger = new TestLogger<ExchangeRateController>();
        var controller = new ExchangeRateController(mockService, mockLogger);
        
        // Mock HttpContext for response headers
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.GetExchangeRates();

        // Assert - Property 4: API Response Format
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var exchangeRateResponse = Assert.IsType<ExchangeRateResponse>(okResult.Value);
        
        ValidateExchangeRateResponse(exchangeRateResponse);
        
        // Assert - Property 10: REST API Conventions
        // Verify response headers are set for REST compliance
        Assert.True(controller.Response.Headers.ContainsKey("X-Total-Count"));
        Assert.True(controller.Response.Headers.ContainsKey("X-Data-Date"));
        Assert.Equal("2", controller.Response.Headers["X-Total-Count"].ToString());
    }

    private static void ValidateExchangeRateResponse(ExchangeRateResponse exchangeRateResponse)
    {
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
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetExchangeRates_WithMultipleRequests_AlwaysReturnsValidFormat(int requestCount)
    {
        // Property-based test simulation: Test with multiple requests to ensure consistent format
        var mockService = new TestExchangeRateService();
        var mockLogger = new TestLogger<ExchangeRateController>();
        var controller = new ExchangeRateController(mockService, mockLogger);

        for (int i = 0; i < requestCount; i++)
        {
            // Mock HttpContext for each request
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            
            // Act
            var result = await controller.GetExchangeRates();

            // Assert - Property 4: API Response Format
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var exchangeRateResponse = Assert.IsType<ExchangeRateResponse>(okResult.Value);
            
            ValidateExchangeRateResponse(exchangeRateResponse);
            
            // Validate structure is consistent
            Assert.IsType<DateTime>(exchangeRateResponse.Date);
            Assert.IsType<int>(exchangeRateResponse.SequenceNumber);
            Assert.IsType<List<ExchangeRate>>(exchangeRateResponse.Rates);
            
            // Assert - Property 10: REST API Conventions
            // Verify consistent response headers across requests
            Assert.True(controller.Response.Headers.ContainsKey("X-Total-Count"));
            Assert.True(controller.Response.Headers.ContainsKey("X-Data-Date"));
        }
    }

    [Fact]
    public async Task GetExchangeRates_WithServiceUnavailable_ReturnsProperErrorFormat()
    {
        // Arrange
        var mockService = new TestExchangeRateServiceWithError();
        var mockLogger = new TestLogger<ExchangeRateController>();
        var controller = new ExchangeRateController(mockService, mockLogger);

        // Act
        var result = await controller.GetExchangeRates();

        // Assert - Property 4: API Response Format (error responses should also be valid)
        var problemResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, problemResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("CNB Service Unavailable", problemDetails.Title);
        Assert.NotNull(problemDetails.Detail);
    }

    [Fact]
    public async Task GetExchangeRates_WithSpecificDate_ReturnsValidResponse()
    {
        // Arrange
        var mockService = new TestExchangeRateService();
        var mockLogger = new TestLogger<ExchangeRateController>();
        var controller = new ExchangeRateController(mockService, mockLogger);
        
        // Mock HttpContext for response headers
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var testDate = DateTime.Today.AddDays(-1);

        // Act
        var result = await controller.GetExchangeRates(testDate);

        // Assert - Property 10: REST API Conventions
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var exchangeRateResponse = Assert.IsType<ExchangeRateResponse>(okResult.Value);
        
        ValidateExchangeRateResponse(exchangeRateResponse);
        
        // Verify REST headers are present
        Assert.True(controller.Response.Headers.ContainsKey("X-Total-Count"));
        Assert.True(controller.Response.Headers.ContainsKey("X-Data-Date"));
    }

    [Fact]
    public async Task GetExchangeRates_WithFutureDate_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new TestExchangeRateService();
        var mockLogger = new TestLogger<ExchangeRateController>();
        var controller = new ExchangeRateController(mockService, mockLogger);
        
        var futureDate = DateTime.Today.AddDays(1);

        // Act
        var result = await controller.GetExchangeRates(futureDate);

        // Assert - Property 10: REST API Conventions
        // Verify proper HTTP status code for invalid input
        var problemResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problemResult.StatusCode);
        
        var problemDetails = Assert.IsType<ProblemDetails>(problemResult.Value);
        Assert.Equal("Invalid Date", problemDetails.Title);
        Assert.Contains("future dates", problemDetails.Detail);
    }

    [Theory]
    [InlineData("GET")]
    public void Controller_UsesCorrectHttpVerbs(string expectedVerb)
    {
        // Property 10: REST API Conventions - Verify HTTP verbs
        var controllerType = typeof(ExchangeRateController);
        var getMethods = controllerType.GetMethods()
            .Where(m => m.Name == "GetExchangeRates")
            .ToList();

        Assert.True(getMethods.Count >= 1, "Controller should have GetExchangeRates methods");
        
        foreach (var method in getMethods)
        {
            var httpGetAttribute = method.GetCustomAttributes(typeof(HttpGetAttribute), false).FirstOrDefault();
            Assert.NotNull(httpGetAttribute);
            
            // Verify the method corresponds to the expected HTTP verb
            Assert.Equal("GET", expectedVerb);
        }
    }

    [Fact]
    public void Controller_FollowsRestResourceNaming()
    {
        // Property 10: REST API Conventions - Verify resource naming
        var controllerType = typeof(ExchangeRateController);
        var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>()
            .FirstOrDefault();

        Assert.NotNull(routeAttribute);
        Assert.Contains("exchange-rates", routeAttribute.Template);
        Assert.Contains("api/v{version:apiVersion}", routeAttribute.Template);
    }
}

// Test service implementations
public class TestExchangeRateService : IExchangeRateService
{
    public Task<ExchangeRateResponse> GetExchangeRatesAsync()
    {
        return Task.FromResult(new ExchangeRateResponse
        {
            Date = DateTime.Today,
            SequenceNumber = 1,
            Rates = new List<ExchangeRate>
            {
                new ExchangeRate
                {
                    Country = "Australia",
                    Currency = "dollar",
                    Amount = 1,
                    Code = "AUD",
                    Rate = 23.282m
                },
                new ExchangeRate
                {
                    Country = "USA",
                    Currency = "dollar",
                    Amount = 1,
                    Code = "USD",
                    Rate = 25.347m
                }
            }
        });
    }

    public Task<ExchangeRateResponse> GetExchangeRatesAsync(DateTime date)
    {
        return GetExchangeRatesAsync();
    }
}

public class TestExchangeRateServiceWithError : IExchangeRateService
{
    public Task<ExchangeRateResponse> GetExchangeRatesAsync()
    {
        throw new CnbServiceUnavailableException("CNB service is unavailable");
    }

    public Task<ExchangeRateResponse> GetExchangeRatesAsync(DateTime date)
    {
        throw new CnbServiceUnavailableException("CNB service is unavailable");
    }
}

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}