using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ExchangeRateApi.Controllers;

/// <summary>
/// Controller for health check operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status of the API</returns>
    /// <response code="200">API is healthy and operational</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        _logger.LogInformation("Health check requested");
        
        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Uptime = DateTime.UtcNow - _startTime
        };

        return Ok(response);
    }

    /// <summary>
    /// Detailed health check with dependencies
    /// </summary>
    /// <returns>Detailed health status including dependencies</returns>
    /// <response code="200">Returns detailed health status</response>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<DetailedHealthResponse> GetDetailedHealth()
    {
        _logger.LogInformation("Detailed health check requested");
        
        var response = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Uptime = DateTime.UtcNow - _startTime,
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = System.Environment.MachineName,
            ProcessorCount = System.Environment.ProcessorCount,
            WorkingSet = System.Environment.WorkingSet / 1024 / 1024 // Convert to MB
        };

        return Ok(response);
    }
}

/// <summary>
/// Basic health response model
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Health status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Timestamp of health check
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Application uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }
}

/// <summary>
/// Detailed health response model with system information
/// </summary>
public class DetailedHealthResponse : HealthResponse
{
    /// <summary>
    /// Current environment (Development, Production, etc.)
    /// </summary>
    public required string Environment { get; set; }

    /// <summary>
    /// Machine name
    /// </summary>
    public required string MachineName { get; set; }

    /// <summary>
    /// Number of processors
    /// </summary>
    public int ProcessorCount { get; set; }

    /// <summary>
    /// Working set memory in MB
    /// </summary>
    public long WorkingSet { get; set; }
}
