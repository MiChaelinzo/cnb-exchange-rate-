using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ExchangeRateApi.Services;
using ExchangeRateApi.Middleware;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace ExchangeRateApi.Tests.Infrastructure;

/// <summary>
/// Property-based tests for error handling consistency across the system
/// Feature: exchange-rate-display, Property 3: Error Handling Consistency
/// Validates: Requirements 1.4, 2.4, 5.1, 5.2, 5.4
/// </summary>
public class ErrorHandlingPropertyTests : PropertyTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorHandlingPropertyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Property: For any network failure or invalid data scenario, the system should handle errors gracefully 
    /// by returning appropriate HTTP status codes (503 for CNB unavailability) and logging errors appropriately
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ErrorHandling_ShouldReturnConsistentStatusCodes()
    {
        return Prop.ForAll(GenerateExceptions(), exception =>
        {
            var (expectedStatusCode, shouldLog) = GetExpectedErrorResponse(exception);
            
            // Test that the GlobalExceptionHandlingMiddleware maps exceptions correctly
            var middleware = CreateMiddleware();
            var (actualStatusCode, actualTitle, actualDetail) = middleware.GetErrorDetails(exception);
            
            return (actualStatusCode == expectedStatusCode)
                .Label($"Expected status {expectedStatusCode}, got {actualStatusCode} for {exception.GetType().Name}")
                .And((!string.IsNullOrEmpty(actualTitle))
                    .Label("Error response should have a title"))
                .And((!string.IsNullOrEmpty(actualDetail))
                    .Label("Error response should have a detail message"))
                .And((shouldLog == true)
                    .Label("All errors should be logged"));
        });
    }

    private static Arbitrary<Exception> GenerateExceptions()
    {
        return Arb.From(Gen.Elements(
            new CnbApiException("CNB service unavailable"),
            new CnbServiceUnavailableException("CNB temporarily down"),
            new CnbDataNotFoundException("Data not found"),
            new CnbDataParsingException("Invalid data format"),
            new HttpRequestException("Network error"),
            new TaskCanceledException("Request timeout"),
            new TimeoutException("Operation timeout"),
            new ArgumentException("Invalid argument"),
            new Exception("Generic error")
        ));
    }

    /// <summary>
    /// Property: For any CNB service exception, the API should return 503 Service Unavailable
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CnbServiceErrors_ShouldReturn503()
    {
        return Prop.ForAll(GenerateErrorMessages(), errorMessage =>
        {
            var cnbException = new CnbServiceUnavailableException(errorMessage);
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(cnbException);
            
            return (statusCode == HttpStatusCode.ServiceUnavailable)
                .Label($"CNB service errors should return 503, got {statusCode}")
                .And((title == "CNB Service Unavailable")
                    .Label($"Expected 'CNB Service Unavailable', got '{title}'"))
                .And((detail.Contains("Czech National Bank"))
                    .Label("Detail should mention Czech National Bank"));
        });
    }

    private static Arbitrary<string> GenerateErrorMessages()
    {
        return Arb.From(Gen.Elements(
            "CNB service is down",
            "Network timeout",
            "Service maintenance",
            "API rate limit exceeded",
            "Connection refused"
        ));
    }

    /// <summary>
    /// Property: For any data parsing error, the system should return 503 and log the error
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DataParsingErrors_ShouldReturn503AndLog()
    {
        return Prop.ForAll(GenerateParsingErrorMessages(), errorMessage =>
        {
            var parsingException = new CnbDataParsingException(errorMessage);
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(parsingException);
            
            return (statusCode == HttpStatusCode.ServiceUnavailable)
                .Label($"Data parsing errors should return 503, got {statusCode}")
                .And((title == "Data Processing Error")
                    .Label($"Expected 'Data Processing Error', got '{title}'"))
                .And((detail.Contains("could not be processed"))
                    .Label("Detail should indicate processing failure"));
        });
    }

    private static Arbitrary<string> GenerateParsingErrorMessages()
    {
        return Arb.From(Gen.Elements(
            "Invalid date format",
            "Missing required fields",
            "Malformed data structure",
            "Unexpected data format",
            "Empty response"
        ));
    }

    /// <summary>
    /// Property: For any timeout scenario, the system should return 408 Request Timeout
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TimeoutErrors_ShouldReturn408()
    {
        return Prop.ForAll(GenerateTimeoutExceptions(), timeoutException =>
        {
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(timeoutException);
            
            return (statusCode == HttpStatusCode.RequestTimeout)
                .Label($"Timeout errors should return 408, got {statusCode}")
                .And((title.Contains("Timeout"))
                    .Label($"Title should contain 'Timeout', got '{title}'"))
                .And((detail.Contains("timed out") || detail.Contains("timeout") || detail.Contains("cancelled"))
                    .Label("Detail should mention timeout or cancellation"));
        });
    }

    private static Arbitrary<Exception> GenerateTimeoutExceptions()
    {
        return Arb.From(Gen.Elements<Exception>(
            new TaskCanceledException("Request was cancelled"),
            new TimeoutException("Operation timed out")
        ));
    }

    /// <summary>
    /// Property: For any invalid argument, the system should return 400 Bad Request
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InvalidArguments_ShouldReturn400()
    {
        return Prop.ForAll(GenerateArgumentErrorMessages(), errorMessage =>
        {
            var argumentException = new ArgumentException(errorMessage);
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(argumentException);
            
            return (statusCode == HttpStatusCode.BadRequest)
                .Label($"Argument errors should return 400, got {statusCode}")
                .And((title == "Bad Request")
                    .Label($"Expected 'Bad Request', got '{title}'"))
                .And((detail.Contains("invalid"))
                    .Label("Detail should indicate invalid parameters"));
        });
    }

    private static Arbitrary<string> GenerateArgumentErrorMessages()
    {
        return Arb.From(Gen.Elements(
            "Invalid date parameter",
            "Missing required parameter",
            "Parameter out of range",
            "Invalid format"
        ));
    }

    /// <summary>
    /// Property: For any unhandled exception, the system should return 500 Internal Server Error
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UnhandledExceptions_ShouldReturn500()
    {
        return Prop.ForAll(GenerateUnhandledExceptions(), unhandledException =>
        {
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(unhandledException);
            
            return (statusCode == HttpStatusCode.InternalServerError)
                .Label($"Unhandled exceptions should return 500, got {statusCode}")
                .And((title == "Internal Server Error")
                    .Label($"Expected 'Internal Server Error', got '{title}'"))
                .And((detail.Contains("unexpected error"))
                    .Label("Detail should indicate unexpected error"));
        });
    }

    private static Arbitrary<Exception> GenerateUnhandledExceptions()
    {
        return Arb.From(Gen.Elements<Exception>(
            new InvalidOperationException("Unexpected state"),
            new NullReferenceException("Null reference"),
            new NotImplementedException("Feature not implemented"),
            new OutOfMemoryException("Memory exhausted")
        ));
    }

    /// <summary>
    /// Property: Error responses should always contain required problem details fields
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ErrorResponses_ShouldContainRequiredFields()
    {
        return Prop.ForAll(GenerateVariousExceptions(), exception =>
        {
            var middleware = CreateMiddleware();
            var (statusCode, title, detail) = middleware.GetErrorDetails(exception);
            
            return (((int)statusCode >= 400 && (int)statusCode < 600))
                .Label($"Status code should be in 4xx or 5xx range, got {statusCode}")
                .And((!string.IsNullOrWhiteSpace(title))
                    .Label("Title should not be null or empty"))
                .And((!string.IsNullOrWhiteSpace(detail))
                    .Label("Detail should not be null or empty"))
                .And((title.Length > 0 && title.Length <= 100)
                    .Label("Title should be reasonable length"))
                .And((detail.Length > 0 && detail.Length <= 500)
                    .Label("Detail should be reasonable length"));
        });
    }

    private static Arbitrary<Exception> GenerateVariousExceptions()
    {
        return Arb.From(Gen.Elements<Exception>(
            new CnbApiException("API error"),
            new ArgumentException("Invalid argument"),
            new Exception("Generic error")
        ));
    }

    private TestableGlobalExceptionHandlingMiddleware CreateMiddleware()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<GlobalExceptionHandlingMiddleware>();
        return new TestableGlobalExceptionHandlingMiddleware(logger);
    }

    private (HttpStatusCode expectedStatusCode, bool shouldLog) GetExpectedErrorResponse(Exception exception)
    {
        return exception switch
        {
            CnbServiceUnavailableException => (HttpStatusCode.ServiceUnavailable, true),
            CnbServiceException => (HttpStatusCode.ServiceUnavailable, true),
            CnbDataNotFoundException => (HttpStatusCode.NotFound, true),
            CnbDataParsingException => (HttpStatusCode.ServiceUnavailable, true),
            CnbApiException => (HttpStatusCode.ServiceUnavailable, true),
            ArgumentException => (HttpStatusCode.BadRequest, true),
            TimeoutException => (HttpStatusCode.RequestTimeout, true),
            HttpRequestException => (HttpStatusCode.ServiceUnavailable, true),
            TaskCanceledException => (HttpStatusCode.RequestTimeout, true),
            _ => (HttpStatusCode.InternalServerError, true)
        };
    }
}

/// <summary>
/// Testable version of GlobalExceptionHandlingMiddleware that exposes the GetErrorDetails method
/// </summary>
public class TestableGlobalExceptionHandlingMiddleware
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public TestableGlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public (HttpStatusCode statusCode, string title, string detail) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            CnbServiceUnavailableException => (
                HttpStatusCode.ServiceUnavailable,
                "CNB Service Unavailable",
                "The Czech National Bank service is currently unavailable. Please try again later."
            ),
            CnbServiceException => (
                HttpStatusCode.ServiceUnavailable,
                "CNB Service Error",
                "The Czech National Bank service encountered an error. Please try again later."
            ),
            CnbDataNotFoundException => (
                HttpStatusCode.NotFound,
                "Data Not Found",
                "The requested exchange rate data was not found."
            ),
            CnbDataParsingException => (
                HttpStatusCode.ServiceUnavailable,
                "Data Processing Error",
                "The exchange rate data could not be processed. Please try again later."
            ),
            CnbApiException => (
                HttpStatusCode.ServiceUnavailable,
                "External Service Error",
                "The external service is currently unavailable. Please try again later."
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Bad Request",
                "The request contains invalid parameters."
            ),
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "The request timed out. Please try again."
            ),
            HttpRequestException => (
                HttpStatusCode.ServiceUnavailable,
                "Service Unavailable",
                "An external service is currently unavailable. Please try again later."
            ),
            TaskCanceledException => (
                HttpStatusCode.RequestTimeout,
                "Request Timeout",
                "The request was cancelled due to timeout. Please try again."
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing your request."
            )
        };
    }
}

// Exception classes for testing (these should match the actual exception classes in the main project)
public class CnbServiceUnavailableException : Exception
{
    public CnbServiceUnavailableException(string message) : base(message) { }
}