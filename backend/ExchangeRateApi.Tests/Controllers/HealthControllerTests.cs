using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using ExchangeRateApi.Controllers;

namespace ExchangeRateApi.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void GetHealth_ReturnsOkResult_WithHealthResponse()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);

        // Act
        var result = controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<ActionResult<HealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var response = Assert.IsType<HealthResponse>(objectResult.Value);
        
        Assert.Equal("Healthy", response.Status);
        Assert.NotNull(response.Version);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.True(response.Uptime.TotalMilliseconds >= 0);
    }

    [Fact]
    public void GetDetailedHealth_ReturnsOkResult_WithDetailedHealthResponse()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);

        // Act
        var result = controller.GetDetailedHealth();

        // Assert
        var okResult = Assert.IsType<ActionResult<DetailedHealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var response = Assert.IsType<DetailedHealthResponse>(objectResult.Value);
        
        Assert.Equal("Healthy", response.Status);
        Assert.NotNull(response.Version);
        Assert.NotNull(response.Environment);
        Assert.NotNull(response.MachineName);
        Assert.True(response.ProcessorCount > 0);
        Assert.True(response.WorkingSet > 0);
        Assert.True(response.Timestamp <= DateTime.UtcNow);
        Assert.True(response.Uptime.TotalMilliseconds >= 0);
    }

    [Fact]
    public void GetHealth_ResponseTimestampIsRecent()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = controller.GetHealth();

        // Assert
        var afterCall = DateTime.UtcNow;
        var okResult = Assert.IsType<ActionResult<HealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var response = Assert.IsType<HealthResponse>(objectResult.Value);
        
        Assert.True(response.Timestamp >= beforeCall);
        Assert.True(response.Timestamp <= afterCall);
    }

    [Fact]
    public void GetHealth_ReturnsStatusCode200()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);

        // Act
        var result = controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<ActionResult<HealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Equal(200, objectResult.StatusCode);
    }

    [Fact]
    public void GetDetailedHealth_ReturnsStatusCode200()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);

        // Act
        var result = controller.GetDetailedHealth();

        // Assert
        var okResult = Assert.IsType<ActionResult<DetailedHealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Equal(200, objectResult.StatusCode);
    }

    [Fact]
    public void GetHealth_ReturnsVersion()
    {
        // Arrange
        var mockLogger = new TestLogger<HealthController>();
        var controller = new HealthController(mockLogger);

        // Act
        var result = controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<ActionResult<HealthResponse>>(result);
        var objectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var response = Assert.IsType<HealthResponse>(objectResult.Value);
        
        Assert.Equal("1.0.0", response.Version);
    }
}
