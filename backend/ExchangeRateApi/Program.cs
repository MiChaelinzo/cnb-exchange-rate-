using ExchangeRateApi.Services;
using ExchangeRateApi.Middleware;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources - supports appsettings.json, environment variables, and command line args
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version")
    );
});

// Add controllers with consistent JSON formatting
builder.Services.AddControllers(options =>
{
    // Ensure consistent response format
    options.ReturnHttpNotAcceptable = true;
    options.RespectBrowserAcceptHeader = true;
    
    // Add consistent response caching headers
    options.CacheProfiles.Add("Default", new CacheProfile
    {
        Duration = 300, // 5 minutes cache for exchange rates
        Location = ResponseCacheLocation.Any,
        VaryByHeader = "Accept,Accept-Encoding"
    });
})
.ConfigureApiBehaviorOptions(options =>
{
    // Customize validation error responses to follow RFC 7807 Problem Details
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Please refer to the errors property for additional details.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

// Add CORS services - supports configuration via appsettings or environment variables
builder.Services.AddCors(options =>
{
    // Get CORS origins from configuration (supports environment variables like CORS__ALLOWEDORIGINS__0=http://localhost:4200)
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                      ?? new[] { "http://localhost:4200" };
    
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    // Fallback policy for development
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure CnbClient options - supports environment variables like CNBAPI__BASEURL
builder.Services.Configure<CnbClientOptions>(
    builder.Configuration.GetSection("CnbApi"));

// Add options validation
builder.Services.AddOptions<CnbClientOptions>()
    .Configure(options => builder.Configuration.GetSection("CnbApi").Bind(options))
    .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl), 
        "CnbApi:BaseUrl configuration is required but not provided")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DailyRatesEndpoint), 
        "CnbApi:DailyRatesEndpoint configuration is required but not provided")
    .ValidateOnStart();

// Register HttpClient for CnbClient with IHttpClientFactory
builder.Services.AddHttpClient<ICnbClient, CnbClient>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "ExchangeRateApi/1.0");
});

// Register application services
builder.Services.AddScoped<IExchangeRateProvider, ExchangeRateProvider>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Use CORS - must be before MapControllers
app.UseCors(app.Environment.IsDevelopment() ? "AllowConfiguredOrigins" : "AllowConfiguredOrigins");

// Map controllers
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
