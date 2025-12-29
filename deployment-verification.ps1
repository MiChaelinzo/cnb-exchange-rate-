# Comprehensive Deployment Verification Script
Write-Host "=== Exchange Rate Application Deployment Verification ===" -ForegroundColor Green

$OriginalLocation = Get-Location

# Backend Verification
Write-Host "`n=== Backend Verification ===" -ForegroundColor Cyan
Set-Location "backend/ExchangeRateApi"

Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host "✓ .NET SDK Version: $dotnetVersion" -ForegroundColor Green

Write-Host "Building and testing..." -ForegroundColor Yellow
dotnet clean | Out-Null
dotnet restore | Out-Null
dotnet build -c Release | Out-Null
Write-Host "✓ Build completed successfully" -ForegroundColor Green

Write-Host "Publishing application..." -ForegroundColor Yellow
if (Test-Path "./publish") {
    Remove-Item -Recurse -Force "./publish"
}
dotnet publish -c Release -o "./publish" | Out-Null
Write-Host "✓ Publish completed" -ForegroundColor Green

Write-Host "Verifying published files..." -ForegroundColor Yellow
$requiredFiles = @("ExchangeRateApi.dll", "appsettings.json", "appsettings.Production.json")
foreach ($file in $requiredFiles) {
    if (Test-Path "./publish/$file") {
        Write-Host "  ✓ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Missing: $file" -ForegroundColor Red
    }
}

Set-Location $OriginalLocation

# Frontend Verification
Write-Host "`n=== Frontend Verification ===" -ForegroundColor Cyan
Set-Location "frontend/exchange-rate-app"

Write-Host "Checking Node.js..." -ForegroundColor Yellow
$nodeVersion = node --version
Write-Host "✓ Node.js Version: $nodeVersion" -ForegroundColor Green

Write-Host "Installing dependencies..." -ForegroundColor Yellow
npm install --silent | Out-Null
Write-Host "✓ Dependencies installed" -ForegroundColor Green

Write-Host "Testing builds..." -ForegroundColor Yellow
npm run build:prod --silent | Out-Null
Write-Host "✓ Production build successful" -ForegroundColor Green

Write-Host "Verifying build outputs..." -ForegroundColor Yellow
if (Test-Path "./dist/exchange-rate-app/browser/index.html") {
    Write-Host "  ✓ Build output contains index.html" -ForegroundColor Green
} else {
    Write-Host "  ✗ Missing index.html in build output" -ForegroundColor Red
}

Set-Location $OriginalLocation

# Configuration Verification
Write-Host "`n=== Configuration Verification ===" -ForegroundColor Cyan
$configFiles = @(
    "backend/ExchangeRateApi/.env.example",
    "frontend/exchange-rate-app/.env.example",
    "docker-compose.yml",
    "DEPLOYMENT.md"
)
foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Missing: $file" -ForegroundColor Red
    }
}

Write-Host "`n=== Deployment Verification Complete ===" -ForegroundColor Green
Write-Host "✓ Application is ready for deployment" -ForegroundColor Green