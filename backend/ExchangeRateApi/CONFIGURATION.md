# Configuration Guide

This document explains how to configure the Exchange Rate API for different environments.

## Configuration Sources

The application loads configuration from multiple sources in the following order (later sources override earlier ones):

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific configuration
3. Environment variables
4. Command line arguments

## Configuration Sections

### CNB API Configuration (`CnbApi`)

Controls how the application connects to the Czech National Bank API:

| Setting | Description | Default | Environment Variable |
|---------|-------------|---------|---------------------|
| `BaseUrl` | CNB API base URL | *Required* | `CNBAPI__BASEURL` |
| `DailyRatesEndpoint` | Endpoint for daily rates | *Required* | `CNBAPI__DAILYRATESENDPOINT` |
| `TimeoutSeconds` | HTTP request timeout | 30 | `CNBAPI__TIMEOUTSECONDS` |
| `MaxRetries` | Maximum retry attempts | 3 | `CNBAPI__MAXRETRIES` |
| `RetryDelayMs` | Delay between retries (ms) | 1000 | `CNBAPI__RETRYDELAYMS` |

### CORS Configuration (`Cors`)

Controls cross-origin resource sharing:

| Setting | Description | Environment Variable |
|---------|-------------|---------------------|
| `AllowedOrigins` | Array of allowed origins | `CORS__ALLOWEDORIGINS__0`, `CORS__ALLOWEDORIGINS__1`, etc. |

## Environment-Specific Configuration

### Development
- Enhanced logging for debugging
- Allows localhost origins for CORS
- Standard timeout and retry settings

### Production
- Reduced logging for performance
- Longer timeouts and more retries for reliability
- CORS origins must be explicitly configured via environment variables

## Environment Variables

Use double underscores (`__`) to represent nested configuration sections:

```bash
# CNB API Configuration
CNBAPI__BASEURL=https://www.cnb.cz/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/
CNBAPI__TIMEOUTSECONDS=60

# CORS Configuration (array)
CORS__ALLOWEDORIGINS__0=https://myapp.com
CORS__ALLOWEDORIGINS__1=https://www.myapp.com

# Logging Configuration
LOGGING__LOGLEVEL__DEFAULT=Information
```

## Docker Configuration

When running in Docker, you can pass environment variables:

```bash
docker run -e CNBAPI__TIMEOUTSECONDS=60 -e CORS__ALLOWEDORIGINS__0=https://myapp.com exchangerateapi
```

## Azure App Service Configuration

In Azure App Service, add application settings:

- `CNBAPI__BASEURL`
- `CNBAPI__TIMEOUTSECONDS`
- `CORS__ALLOWEDORIGINS__0`

## Validation

The application validates that required configuration values are provided:

- `CnbApi:BaseUrl` - Must not be empty
- `CnbApi:DailyRatesEndpoint` - Must not be empty

If required configuration is missing, the application will fail to start with a descriptive error message.

## Security Considerations

- Never commit sensitive configuration to source control
- Use environment variables or secure configuration providers for production
- Regularly review and update CORS allowed origins
- Consider using Azure Key Vault or similar for sensitive settings in production