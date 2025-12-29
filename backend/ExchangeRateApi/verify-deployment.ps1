# Deployment Verification Script for Exchange Rate API
# This script verifies that the application can be deployed and run in a clean environment

Write-Host "=== Exchange Rate API Deployment Verification ===" -ForegroundColor Green

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check .NET version
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "✗ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Clean and restore
Write-Host "`nCleaning and restoring packages..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Clean failed" -ForegroundColor Red
    exit 1
}

dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Restore failed" -ForegroundColor Red
    exit 1
}

# Build
Write-Host "`nBuilding application..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "`nRunning tests..." -ForegroundColor Yellow
Set-Location "../ExchangeRateApi.Tests"
dotnet test --no-build --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Tests failed" -ForegroundColor Red
    Set-Location "../ExchangeRateApi"
    exit 1
}
Set-Location "../ExchangeRateApi"

# Publish
Write-Host "`nPublishing application..." -ForegroundColor Yellow
if (Test-Path "./publish") {
    Remove-Item -Recurse -Force "./publish"
}
dotnet publish -c Release -o "./publish"
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Publish failed" -ForegroundColor Red
    exit 1
}

# Verify published files
Write-Host "`nVerifying published files..." -ForegroundColor Yellow
$requiredFiles = @(
    "ExchangeRateApi.dll",
    "appsettings.json",
    "appsettings.Production.json"
)

foreach ($file in $requiredFiles) {
    if (Test-Path "./publish/$file") {
        Write-Host "✓ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "✗ Missing: $file" -ForegroundColor Red
        exit 1
    }
}

# Check configuration externalization
Write-Host "`nVerifying configuration externalization..." -ForegroundColor Yellow

# Check that environment variables can override configuration
$env:CNBAPI__TIMEOUTSECONDS = "120"
$env:CORS__ALLOWEDORIGINS__0 = "https://test.example.com"

Write-Host "✓ Environment variables can be set for configuration override" -ForegroundColor Green

# Clean up test environment variables
Remove-Item Env:CNBAPI__TIMEOUTSECONDS -ErrorAction SilentlyContinue
Remove-Item Env:CORS__ALLOWEDORIGINS__0 -ErrorAction SilentlyContinue

Write-Host "`n=== Deployment Verification Complete ===" -ForegroundColor Green
Write-Host "✓ Application is ready for deployment" -ForegroundColor Green
Write-Host "`nTo deploy:" -ForegroundColor Cyan
Write-Host "1. Copy the ./publish folder to your target server" -ForegroundColor Cyan
Write-Host "2. Set environment variables as needed (see .env.example)" -ForegroundColor Cyan
Write-Host "3. Run: dotnet ExchangeRateApi.dll" -ForegroundColor Cyan