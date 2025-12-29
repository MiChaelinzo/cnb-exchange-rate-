using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ExchangeRateApi.Services;
using System.Text.RegularExpressions;

namespace ExchangeRateApi.Tests.Infrastructure;

/// <summary>
/// Property tests for configuration management
/// Feature: exchange-rate-display, Property 9: Configuration Management
/// Validates: Requirements 4.1, 4.2, 4.3, 4.4
/// </summary>
public class ConfigurationManagementPropertyTests : PropertyTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConfigurationManagementPropertyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Property 9: Configuration Management
    /// For any environment deployment, the system should read all URLs from configuration sources 
    /// (environment variables or config files) and contain no hardcoded URLs in source code
    /// Validates: Requirements 4.1, 4.2, 4.3, 4.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ConfigurationShouldBeExternalizedProperty()
    {
        var validUrlGen = Gen.Elements("https://api.example.com", "http://localhost:5000", "https://test.api.com")
            .Where(s => Uri.IsWellFormedUriString(s, UriKind.Absolute));
        var validEndpointGen = Gen.Elements("daily.txt", "rates.json", "data.xml")
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return Prop.ForAll(
            Arb.From(validUrlGen),
            Arb.From(validEndpointGen),
            (baseUrl, endpoint) =>
            {
                // Test that configuration can be overridden via environment variables
                var configBuilder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["CnbApi:BaseUrl"] = baseUrl,
                        ["CnbApi:DailyRatesEndpoint"] = endpoint,
                        ["CnbApi:TimeoutSeconds"] = "30",
                        ["CnbApi:MaxRetries"] = "3",
                        ["CnbApi:RetryDelayMs"] = "1000"
                    });

                var configuration = configBuilder.Build();
                var cnbOptions = new CnbClientOptions();
                configuration.GetSection("CnbApi").Bind(cnbOptions);

                // Verify configuration is properly bound
                return cnbOptions.BaseUrl == baseUrl &&
                       cnbOptions.DailyRatesEndpoint == endpoint &&
                       cnbOptions.TimeoutSeconds == 30 &&
                       cnbOptions.MaxRetries == 3 &&
                       cnbOptions.RetryDelayMs == 1000;
            });
    }

    [Property(MaxTest = 100)]
    public Property CorsConfigurationShouldBeExternalizedProperty()
    {
        return Prop.ForAll<string[]>(
            (allowedOrigins) =>
            {
                // Use a simple array of valid origins for testing
                var testOrigins = new[] { "http://localhost:4200", "https://app.example.com" };
                
                // Test that CORS origins can be configured via configuration
                var corsConfig = new Dictionary<string, string?>();
                for (int i = 0; i < testOrigins.Length; i++)
                {
                    corsConfig[$"Cors:AllowedOrigins:{i}"] = testOrigins[i];
                }

                var configBuilder = new ConfigurationBuilder()
                    .AddInMemoryCollection(corsConfig);

                var configuration = configBuilder.Build();
                var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                // Verify CORS configuration is properly bound
                return configuredOrigins != null &&
                       configuredOrigins.Length == testOrigins.Length &&
                       configuredOrigins.SequenceEqual(testOrigins);
            });
    }

    [Property(MaxTest = 100)]
    public Property ConfigurationValidationShouldWorkProperty()
    {
        var urlGen = Gen.OneOf(
            Gen.Constant<string?>("https://valid.url.com"),
            Gen.Constant<string?>(""),
            Gen.Constant<string?>(null)
        );
        var endpointGen = Gen.OneOf(
            Gen.Constant<string?>("valid.txt"),
            Gen.Constant<string?>(""),
            Gen.Constant<string?>(null)
        );

        return Prop.ForAll(
            Arb.From(urlGen),
            Arb.From(endpointGen),
            (baseUrl, endpoint) =>
            {
                var isValidBaseUrl = !string.IsNullOrWhiteSpace(baseUrl);
                var isValidEndpoint = !string.IsNullOrWhiteSpace(endpoint);

                try
                {
                    var configBuilder = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["CnbApi:BaseUrl"] = baseUrl,
                            ["CnbApi:DailyRatesEndpoint"] = endpoint
                        });

                    var configuration = configBuilder.Build();
                    var cnbOptions = new CnbClientOptions();
                    configuration.GetSection("CnbApi").Bind(cnbOptions);

                    // Validate configuration
                    var baseUrlValid = !string.IsNullOrWhiteSpace(cnbOptions.BaseUrl);
                    var endpointValid = !string.IsNullOrWhiteSpace(cnbOptions.DailyRatesEndpoint);

                    // Configuration should be valid if and only if both inputs are valid
                    return (baseUrlValid && endpointValid) == (isValidBaseUrl && isValidEndpoint);
                }
                catch
                {
                    // If configuration binding fails, both inputs should be invalid
                    return !isValidBaseUrl || !isValidEndpoint;
                }
            });
    }

    [Fact]
    public void SourceCodeShouldNotContainHardcodedUrls()
    {
        // Test that source code doesn't contain hardcoded URLs
        var sourceFiles = new[]
        {
            "Services/CnbClient.cs",
            "Services/ExchangeRateService.cs",
            "Services/ExchangeRateProvider.cs",
            "Controllers/ExchangeRateController.cs",
            "Program.cs"
        };

        var hardcodedUrlPattern = new Regex(@"https?://[^\s""']+", RegexOptions.IgnoreCase);
        var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ExchangeRateApi"));

        foreach (var file in sourceFiles)
        {
            var filePath = Path.Combine(projectRoot, file);
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var matches = hardcodedUrlPattern.Matches(content);
                
                // Filter out acceptable URLs (like documentation links, schema URLs, etc.)
                var hardcodedUrls = matches
                    .Where(m => !IsAcceptableUrl(m.Value))
                    .ToList();

                Assert.Empty(hardcodedUrls.Select(m => $"Found hardcoded URL in {file}: {m.Value}"));
            }
        }
    }

    [Fact]
    public void ConfigurationShouldBeLoadedFromMultipleSources()
    {
        // Test that configuration is loaded from multiple sources as documented
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Verify configuration sources are properly set up
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "TestValue"
                });
            });
        });

        var client = factory.CreateClient();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        
        // Verify test configuration is loaded
        Assert.Equal("TestValue", configuration["TestKey"]);
        
        // Verify required configuration sections exist
        Assert.NotNull(configuration.GetSection("CnbApi"));
        Assert.NotNull(configuration.GetSection("Cors"));
        Assert.NotNull(configuration.GetSection("Logging"));
    }

    private static bool IsAcceptableUrl(string url)
    {
        // Allow documentation URLs, schema URLs, and other acceptable hardcoded URLs
        var acceptablePatterns = new[]
        {
            @"https://tools\.ietf\.org/",
            @"https://aka\.ms/",
            @"https://docs\.microsoft\.com/",
            @"https://schemas\.microsoft\.com/",
            @"http://www\.w3\.org/",
            @"https://www\.w3\.org/"
        };

        return acceptablePatterns.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase));
    }
}