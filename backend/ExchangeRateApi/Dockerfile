# Multi-stage Dockerfile for Exchange Rate API
# This Dockerfile builds and packages the .NET API for deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["ExchangeRateApi.csproj", "."]
RUN dotnet restore "ExchangeRateApi.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "ExchangeRateApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ExchangeRateApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/exchangerates || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ExchangeRateApi.dll"]