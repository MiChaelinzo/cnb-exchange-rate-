# Deployment Verification Script for Exchange Rate Frontend
# This script verifies that the Angular application can be built and deployed

Write-Host "=== Exchange Rate Frontend Deployment Verification ===" -ForegroundColor Green

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check Node.js version
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Node.js Version: $nodeVersion" -ForegroundColor Green
} else {
    Write-Host "✗ Node.js not found. Please install Node.js 18 or higher" -ForegroundColor Red
    exit 1
}

# Check npm version
$npmVersion = npm --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ npm Version: $npmVersion" -ForegroundColor Green
} else {
    Write-Host "✗ npm not found" -ForegroundColor Red
    exit 1
}

# Check Angular CLI
$ngVersion = ng version --skip-git 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Angular CLI available" -ForegroundColor Green
} else {
    Write-Host "! Angular CLI not found globally, using local version" -ForegroundColor Yellow
}

# Install dependencies
Write-Host "`nInstalling dependencies..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ npm install failed" -ForegroundColor Red
    exit 1
}

# Test environment configuration generation
Write-Host "`nTesting environment configuration..." -ForegroundColor Yellow

# Test development build
Write-Host "Building for development..." -ForegroundColor Cyan
npm run build:dev
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Development build failed" -ForegroundColor Red
    exit 1
}

# Test staging build with custom API URL
Write-Host "Building for staging with custom API URL..." -ForegroundColor Cyan
$env:API_BASE_URL = "https://staging-api.test.com/api"
npm run build:staging
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Staging build failed" -ForegroundColor Red
    exit 1
}

# Test production build
Write-Host "Building for production..." -ForegroundColor Cyan
$env:API_BASE_URL = "https://api.production.com"
npm run build:prod
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Production build failed" -ForegroundColor Red
    exit 1
}

# Clean up test environment variable
Remove-Item Env:API_BASE_URL -ErrorAction SilentlyContinue

# Verify build outputs
Write-Host "`nVerifying build outputs..." -ForegroundColor Yellow

if (Test-Path "./dist/exchange-rate-app") {
    $buildFiles = Get-ChildItem "./dist/exchange-rate-app" -File
    Write-Host "✓ Build output contains $($buildFiles.Count) files" -ForegroundColor Green
    
    # Check for essential files
    $essentialFiles = @("index.html")
    foreach ($file in $essentialFiles) {
        if (Test-Path "./dist/exchange-rate-app/$file") {
            Write-Host "✓ Found: $file" -ForegroundColor Green
        } else {
            Write-Host "✗ Missing: $file" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "✗ Build output directory not found" -ForegroundColor Red
    exit 1
}

# Verify environment configuration
Write-Host "`nVerifying environment configuration..." -ForegroundColor Yellow

$envFiles = @(
    "src/environments/environment.ts",
    "src/environments/environment.staging.ts", 
    "src/environments/environment.prod.ts"
)

foreach ($file in $envFiles) {
    if (Test-Path $file) {
        Write-Host "✓ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "✗ Missing: $file" -ForegroundColor Red
        exit 1
    }
}

# Test configuration script
Write-Host "`nTesting configuration script..." -ForegroundColor Yellow
node scripts/set-env.js development
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Configuration script failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Frontend Deployment Verification Complete ===" -ForegroundColor Green
Write-Host "✓ Application is ready for deployment" -ForegroundColor Green
Write-Host "`nDeployment options:" -ForegroundColor Cyan
Write-Host "1. Static hosting: Deploy ./dist/exchange-rate-app folder to web server" -ForegroundColor Cyan
Write-Host "2. Docker: Use provided Dockerfile for containerized deployment" -ForegroundColor Cyan
Write-Host "3. CDN: Upload build files to CDN with proper MIME types" -ForegroundColor Cyan
Write-Host "`nEnvironment configuration:" -ForegroundColor Cyan
Write-Host "- Set API_BASE_URL environment variable before building" -ForegroundColor Cyan
Write-Host "- Use npm run build:prod for production builds" -ForegroundColor Cyan