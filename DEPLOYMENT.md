# Deployment Guide

This guide provides comprehensive instructions for deploying the Exchange Rate Display application in various environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Local Development Deployment](#local-development-deployment)
- [Docker Deployment](#docker-deployment)
- [Production Deployment](#production-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Configuration Management](#configuration-management)
- [Monitoring and Health Checks](#monitoring-and-health-checks)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

- **Operating System**: Windows 10+, macOS 10.15+, or Linux (Ubuntu 18.04+)
- **Memory**: 4GB RAM minimum, 8GB recommended for production
- **Disk Space**: 2GB free space for build artifacts and dependencies
- **Network**: Internet access for downloading dependencies and CNB API access

### Required Software

#### For Native Deployment
- **.NET 9.0 Runtime** - For running the backend API
- **Web Server** (IIS, Nginx, Apache) - For hosting the frontend
- **Reverse Proxy** (optional) - For load balancing and SSL termination

#### For Docker Deployment
- **Docker Engine** 20.10+ - For containerized deployment
- **Docker Compose** 2.0+ - For multi-container orchestration

## Local Development Deployment

### Quick Start

1. **Clone and build both projects**:
   ```bash
   # Backend
   cd backend/ExchangeRateApi
   dotnet restore && dotnet build -c Release
   
   # Frontend
   cd ../../frontend/exchange-rate-app
   npm install && npm run build:prod
   ```

2. **Run verification scripts**:
   ```bash
   # Backend verification
   cd ../../backend/ExchangeRateApi
   ./verify-deployment.ps1
   
   # Frontend verification
   cd ../../frontend/exchange-rate-app
   ./verify-deployment.ps1
   ```

3. **Start services**:
   ```bash
   # Terminal 1 - Backend
   cd backend/ExchangeRateApi
   dotnet run --urls "http://localhost:5000"
   
   # Terminal 2 - Frontend (serve built files)
   cd frontend/exchange-rate-app
   npx http-server dist/exchange-rate-app -p 4200
   ```

## Docker Deployment

### Single Command Deployment

```bash
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up --build -d
```

### Custom Configuration

1. **Create environment-specific compose file**:
   ```yaml
   # docker-compose.prod.yml
   version: '3.8'
   services:
     api:
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - CNBAPI__TIMEOUTSECONDS=120
         - CORS__ALLOWEDORIGINS__0=https://yourdomain.com
       ports:
         - "80:8080"
     
     web:
       build:
         args:
           - API_BASE_URL=https://api.yourdomain.com
       ports:
         - "443:8080"
   ```

2. **Deploy with custom configuration**:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
   ```

### Docker Commands Reference

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Update and restart
docker-compose pull && docker-compose up -d

# Clean up
docker-compose down -v --rmi all
```

## Production Deployment

### Backend Deployment

#### Option 1: Self-Contained Deployment

```bash
cd backend/ExchangeRateApi

# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-win

# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish-linux
```

#### Option 2: Framework-Dependent Deployment

```bash
cd backend/ExchangeRateApi

# Requires .NET runtime on target server
dotnet publish -c Release -o ./publish
```

#### IIS Deployment (Windows)

1. **Install ASP.NET Core Hosting Bundle** on the server
2. **Create IIS Application**:
   - Copy published files to `C:\inetpub\wwwroot\exchange-rate-api`
   - Create IIS application pointing to this directory
   - Set application pool to "No Managed Code"

3. **Configure web.config**:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <system.webServer>
       <handlers>
         <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
       </handlers>
       <aspNetCore processPath="dotnet" arguments=".\ExchangeRateApi.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" />
     </system.webServer>
   </configuration>
   ```

#### Linux Deployment with Systemd

1. **Copy published files** to `/var/www/exchange-rate-api`

2. **Create systemd service** (`/etc/systemd/system/exchange-rate-api.service`):
   ```ini
   [Unit]
   Description=Exchange Rate API
   After=network.target
   
   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /var/www/exchange-rate-api/ExchangeRateApi.dll
   Restart=always
   RestartSec=5
   KillSignal=SIGINT
   SyslogIdentifier=exchange-rate-api
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:5000
   
   [Install]
   WantedBy=multi-user.target
   ```

3. **Enable and start service**:
   ```bash
   sudo systemctl enable exchange-rate-api
   sudo systemctl start exchange-rate-api
   sudo systemctl status exchange-rate-api
   ```

### Frontend Deployment

#### Static File Hosting

1. **Build for production**:
   ```bash
   cd frontend/exchange-rate-app
   API_BASE_URL=https://api.yourdomain.com npm run build:prod
   ```

2. **Deploy to web server**:
   ```bash
   # Copy dist files to web server
   cp -r dist/exchange-rate-app/* /var/www/html/
   ```

#### Nginx Configuration

```nginx
server {
    listen 80;
    server_name yourdomain.com;
    root /var/www/html;
    index index.html;

    # Handle Angular routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Proxy API requests to backend
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Cloud Deployment

### Azure App Service

#### Backend Deployment

1. **Create App Service** in Azure Portal
2. **Configure deployment**:
   ```bash
   # Using Azure CLI
   az webapp deployment source config-zip \
     --resource-group myResourceGroup \
     --name myapp-api \
     --src ./publish.zip
   ```

3. **Set application settings**:
   ```bash
   az webapp config appsettings set \
     --resource-group myResourceGroup \
     --name myapp-api \
     --settings CNBAPI__TIMEOUTSECONDS=120 \
                CORS__ALLOWEDORIGINS__0=https://myapp.azurewebsites.net
   ```

#### Frontend Deployment

1. **Build and deploy to Static Web Apps**:
   ```bash
   # Install Azure Static Web Apps CLI
   npm install -g @azure/static-web-apps-cli
   
   # Deploy
   swa deploy ./dist/exchange-rate-app --env production
   ```

### AWS Deployment

#### Backend (Elastic Beanstalk)

1. **Create deployment package**:
   ```bash
   cd backend/ExchangeRateApi
   dotnet publish -c Release -o ./aws-deploy
   cd aws-deploy && zip -r ../deployment.zip .
   ```

2. **Deploy to Elastic Beanstalk**:
   ```bash
   # Using AWS CLI
   aws elasticbeanstalk create-application-version \
     --application-name exchange-rate-api \
     --version-label v1.0 \
     --source-bundle S3Bucket=my-bucket,S3Key=deployment.zip
   ```

#### Frontend (S3 + CloudFront)

1. **Build and upload to S3**:
   ```bash
   cd frontend/exchange-rate-app
   API_BASE_URL=https://api.myapp.com npm run build:prod
   aws s3 sync ./dist/exchange-rate-app s3://my-frontend-bucket --delete
   ```

2. **Configure CloudFront** for SPA routing

## Configuration Management

### Environment Variables

#### Backend Configuration

```bash
# Production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export CNBAPI__BASEURL=https://www.cnb.cz/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/
export CNBAPI__TIMEOUTSECONDS=120
export CNBAPI__MAXRETRIES=5
export CORS__ALLOWEDORIGINS__0=https://yourdomain.com
export LOGGING__LOGLEVEL__DEFAULT=Warning
```

#### Frontend Configuration

```bash
# Set API URL before building
export API_BASE_URL=https://api.yourdomain.com
npm run build:prod
```

### Configuration Validation

Both projects include configuration validation:

- **Backend**: Validates required CNB API settings on startup
- **Frontend**: Validates API base URL format during build

### Secrets Management

For production deployments:

1. **Use secure configuration providers**:
   - Azure Key Vault
   - AWS Secrets Manager
   - HashiCorp Vault

2. **Never commit secrets** to source control
3. **Use environment variables** for configuration
4. **Rotate secrets regularly**

## Monitoring and Health Checks

### Health Check Endpoints

#### Backend Health Check

```bash
# Check API health
curl -f http://localhost:5000/api/exchangerates

# Expected response: JSON with exchange rates or appropriate error
```

#### Frontend Health Check

```bash
# Check frontend availability
curl -f http://localhost:4200/

# Expected response: HTML content
```

### Docker Health Checks

Health checks are built into the Docker containers:

```bash
# Check container health
docker ps
# Look for "healthy" status

# View health check logs
docker inspect --format='{{json .State.Health}}' container-name
```

### Monitoring Setup

#### Application Insights (Azure)

```csharp
// Add to Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

#### CloudWatch (AWS)

```bash
# Install CloudWatch agent
# Configure custom metrics for API response times
```

### Log Management

#### Structured Logging

The backend uses structured logging with Serilog:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Log Aggregation

For production, consider:
- **ELK Stack** (Elasticsearch, Logstash, Kibana)
- **Azure Monitor**
- **AWS CloudWatch Logs**

## Troubleshooting

### Common Deployment Issues

#### Backend Issues

**Issue**: Application fails to start
```bash
# Check logs
docker logs container-name
# or
journalctl -u exchange-rate-api -f
```

**Solution**: Verify configuration and dependencies

**Issue**: CNB API connectivity problems
```bash
# Test CNB API directly
curl -I https://www.cnb.cz/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/daily.txt
```

**Solution**: Check network connectivity and firewall rules

#### Frontend Issues

**Issue**: API calls fail with CORS errors
**Solution**: Update CORS configuration in backend

**Issue**: Routing doesn't work on refresh
**Solution**: Configure web server for SPA routing (try_files in nginx)

#### Docker Issues

**Issue**: Container fails to build
```bash
# Build with verbose output
docker build --no-cache --progress=plain .
```

**Issue**: Services can't communicate
```bash
# Check network connectivity
docker network ls
docker network inspect exchange-rate-network
```

### Performance Optimization

#### Backend Optimization

1. **Enable response compression**
2. **Configure connection pooling**
3. **Implement caching** for CNB API responses
4. **Use async/await** patterns

#### Frontend Optimization

1. **Enable gzip compression**
2. **Implement lazy loading**
3. **Optimize bundle size**
4. **Use CDN** for static assets

### Security Considerations

1. **Use HTTPS** in production
2. **Configure proper CORS** origins
3. **Implement rate limiting**
4. **Regular security updates**
5. **Use non-root containers**
6. **Scan for vulnerabilities**

### Backup and Recovery

1. **Configuration backup**: Store configuration in version control
2. **Database backup**: Not applicable (stateless application)
3. **Disaster recovery**: Use infrastructure as code (Terraform, ARM templates)

---

For additional support, refer to the main [README.md](README.md) file or create an issue in the project repository.