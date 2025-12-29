using FsCheck;
using FsCheck.Xunit;
using ExchangeRateApi.Controllers;
using ExchangeRateApi.Models;
using ExchangeRateApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRateApi.Tests.Infrastructure;

/// <summary>
/// Feature: exchange-rate-display, Property 10: REST API Conventions
/// Tests that REST endpoints follow proper conventions for HTTP verbs, resource naming, and status codes
/// **Validates: Requirements 6.2**
/// </summary>
public class RestApiConventionsPropertyTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public Property RestEndpoint_ShouldFollowNamingConventions()
    {
        return Prop.ForAll<int>(dummy =>
        {
            // Test that our controller follows REST naming conventions
            var controllerType = typeof(ExchangeRateController);
            var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>()
                .FirstOrDefault();

            if (routeAttribute == null) return false;
            
            var template = routeAttribute.Template;
            
            return template.Contains("api/v{version:apiVersion}/exchange-rates") &&
                   template.Contains("exchange-rates") && // Resource should be plural
                   !template.Contains("_") && // Should use hyphens, not underscores
                   template.StartsWith("api/"); // Should start with api/
        });
    }

    [Property(MaxTest = 100)]
    public Property RestEndpoint_ShouldUseCorrectHttpVerbs()
    {
        return Prop.ForAll<int>(methodIndex =>
        {
            var controllerType = typeof(ExchangeRateController);
            var publicMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controllerType)
                .ToArray();

            if (publicMethods.Length == 0) return true;

            var method = publicMethods[Math.Abs(methodIndex) % publicMethods.Length];
            
            // GET methods should have HttpGet attribute
            if (method.Name.StartsWith("Get"))
            {
                var httpGetAttribute = method.GetCustomAttribute<HttpGetAttribute>();
                return httpGetAttribute != null;
            }
            
            // POST methods should have HttpPost attribute (if any exist)
            if (method.Name.StartsWith("Post") || method.Name.StartsWith("Create"))
            {
                var httpPostAttribute = method.GetCustomAttribute<HttpPostAttribute>();
                return httpPostAttribute != null;
            }
            
            // PUT methods should have HttpPut attribute (if any exist)
            if (method.Name.StartsWith("Put") || method.Name.StartsWith("Update"))
            {
                var httpPutAttribute = method.GetCustomAttribute<HttpPutAttribute>();
                return httpPutAttribute != null;
            }
            
            // DELETE methods should have HttpDelete attribute (if any exist)
            if (method.Name.StartsWith("Delete"))
            {
                var httpDeleteAttribute = method.GetCustomAttribute<HttpDeleteAttribute>();
                return httpDeleteAttribute != null;
            }

            return true; // Other methods are acceptable
        });
    }

    [Property(MaxTest = 100)]
    public Property RestResponse_ShouldHaveCorrectContentType()
    {
        return Prop.ForAll<bool>(useValidService =>
        {
            // Arrange
            IExchangeRateService mockService = useValidService ? new TestExchangeRateService() : new TestExchangeRateServiceWithError();
            var mockLogger = new TestLogger<ExchangeRateController>();
            var controller = new ExchangeRateController(mockService, mockLogger);
            
            // Mock HttpContext
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Check that controller is configured to produce JSON
            var controllerType = typeof(ExchangeRateController);
            var producesAttribute = controllerType.GetCustomAttribute<ProducesAttribute>();
            
            return producesAttribute != null && 
                   producesAttribute.ContentTypes.Contains("application/json");
        });
    }

    [Property(MaxTest = 100)]
    public Property RestResponse_ShouldHaveCorrectStatusCodes()
    {
        return Prop.ForAll<bool>(simulateError =>
        {
            try
            {
                // Arrange
                IExchangeRateService mockService = simulateError ? new TestExchangeRateServiceWithError() : new TestExchangeRateService();
                var mockLogger = new TestLogger<ExchangeRateController>();
                var controller = new ExchangeRateController(mockService, mockLogger);
                
                // Mock HttpContext
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                // Act & Assert
                var task = controller.GetExchangeRates();
                task.Wait();
                var result = task.Result;

                if (simulateError)
                {
                    // Should return 503 for service unavailable
                    var problemResult = result.Result as ObjectResult;
                    return problemResult != null && problemResult.StatusCode == 503;
                }
                else
                {
                    // Should return 200 for success
                    var okResult = result.Result as OkObjectResult;
                    return okResult != null;
                }
            }
            catch
            {
                return false;
            }
        });
    }

    [Property(MaxTest = 100)]
    public Property RestResponse_ShouldIncludeStandardHeaders()
    {
        return Prop.ForAll<int>(requestId =>
        {
            try
            {
                // Arrange
                var mockService = new TestExchangeRateService();
                var mockLogger = new TestLogger<ExchangeRateController>();
                var controller = new ExchangeRateController(mockService, mockLogger);
                
                // Mock HttpContext
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                // Act
                var task = controller.GetExchangeRates();
                task.Wait();
                var result = task.Result;

                // Assert - REST APIs should include metadata headers
                var okResult = result.Result as OkObjectResult;
                if (okResult != null)
                {
                    // Check that standard REST headers are set
                    return controller.Response.Headers.ContainsKey("X-Total-Count") &&
                           controller.Response.Headers.ContainsKey("X-Data-Date");
                }
                
                return true; // Error responses don't need these headers
            }
            catch
            {
                return false;
            }
        });
    }

    [Property(MaxTest = 100)]
    public Property RestEndpoint_ShouldHaveProperDocumentation()
    {
        return Prop.ForAll<string>(methodName =>
        {
            var controllerType = typeof(ExchangeRateController);
            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controllerType);

            foreach (var method in methods)
            {
                // Each public method should have ProducesResponseType attributes for different status codes
                var responseTypeAttributes = method.GetCustomAttributes<ProducesResponseTypeAttribute>();
                
                if (method.Name.StartsWith("Get"))
                {
                    // GET methods should document at least 200 and error status codes
                    var statusCodes = responseTypeAttributes.Select(attr => attr.StatusCode).ToList();
                    
                    if (!statusCodes.Contains(200))
                        return false; // Should document success case
                        
                    if (!statusCodes.Any(code => code >= 400))
                        return false; // Should document error cases
                }
            }

            return true;
        });
    }

    [Property(MaxTest = 100)]
    public Property RestEndpoint_ShouldValidateInputParameters()
    {
        return Prop.ForAll<DateTime>(testDate =>
        {
            try
            {
                // Arrange
                var mockService = new TestExchangeRateService();
                var mockLogger = new TestLogger<ExchangeRateController>();
                var controller = new ExchangeRateController(mockService, mockLogger);
                
                // Mock HttpContext
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                // Test that future dates are properly rejected
                if (testDate.Date > DateTime.Today)
                {
                    var task = controller.GetExchangeRates(testDate);
                    task.Wait();
                    var result = task.Result;
                    
                    var problemResult = result.Result as ObjectResult;
                    return problemResult != null && problemResult.StatusCode == 400;
                }
                
                return true; // Valid dates should be accepted
            }
            catch
            {
                return false;
            }
        });
    }

    [Fact]
    public void RestController_ShouldHaveApiControllerAttribute()
    {
        // REST controllers should use [ApiController] attribute for automatic model validation
        var controllerType = typeof(ExchangeRateController);
        var apiControllerAttribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();
        
        Assert.NotNull(apiControllerAttribute);
    }

    [Fact]
    public void RestController_ShouldHaveVersioning()
    {
        // REST APIs should support versioning
        var controllerType = typeof(ExchangeRateController);
        var apiVersionAttribute = controllerType.GetCustomAttribute<Asp.Versioning.ApiVersionAttribute>();
        
        Assert.NotNull(apiVersionAttribute);
        Assert.Contains("1.0", apiVersionAttribute.Versions.Select(v => v.ToString()));
    }

    [Fact]
    public void RestController_ShouldSpecifyConsumesAndProduces()
    {
        // REST controllers should specify what they consume and produce
        var controllerType = typeof(ExchangeRateController);
        
        var producesAttribute = controllerType.GetCustomAttribute<ProducesAttribute>();
        var consumesAttribute = controllerType.GetCustomAttribute<ConsumesAttribute>();
        
        Assert.NotNull(producesAttribute);
        Assert.NotNull(consumesAttribute);
        Assert.Contains("application/json", producesAttribute.ContentTypes);
        Assert.Contains("application/json", consumesAttribute.ContentTypes);
    }
}

// Test service implementations for property tests
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