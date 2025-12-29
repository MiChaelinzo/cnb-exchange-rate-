using ExchangeRateApi.Services;
using System.Net;
using System.Text.Json;

namespace ExchangeRateApi.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, detail) = GetErrorDetails(exception);
        
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = title,
            status = (int)statusCode,
            detail = detail,
            instance = context.Request.Path,
            traceId = context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private (HttpStatusCode statusCode, string title, string detail) GetErrorDetails(Exception exception)
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