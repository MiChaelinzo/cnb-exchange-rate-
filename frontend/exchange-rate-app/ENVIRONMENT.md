# Environment Configuration

This document describes how to configure the Angular frontend for different environments.

## Environment Files

The application supports three environments:

- **Development** (`environment.ts`): Local development with backend running on localhost
- **Staging** (`environment.staging.ts`): Staging environment with staging API
- **Production** (`environment.prod.ts`): Production environment with relative API URLs

## Environment Variables

The following environment variables can be used to override default configurations:

### API_BASE_URL
- **Description**: Base URL for the backend API
- **Default Values**:
  - Development: `http://localhost:5000/api`
  - Staging: `https://staging-api.example.com/api`
  - Production: `/api` (relative URL)
- **Example**: `API_BASE_URL=https://api.mycompany.com/v1`

## Building for Different Environments

### Development Build
```bash
npm run build:dev
# or with custom API URL
API_BASE_URL=http://localhost:8080/api npm run build:dev
```

### Staging Build
```bash
npm run build:staging
# or with custom API URL
API_BASE_URL=https://staging.mycompany.com/api npm run build:staging
```

### Production Build
```bash
npm run build:prod
# or with custom API URL
API_BASE_URL=https://api.mycompany.com npm run build:prod
```

## Serving for Different Environments

### Development Server
```bash
npm run serve:dev
# or with custom API URL
API_BASE_URL=http://localhost:8080/api npm run serve:dev
```

### Staging Server
```bash
npm run serve:staging
# or with custom API URL
API_BASE_URL=https://staging.mycompany.com/api npm run serve:staging
```

## Environment File Generation

The `scripts/set-env.js` script automatically generates environment files based on:
1. The target environment (development, staging, production)
2. Environment variables (if set)
3. Default values for each environment

### Manual Environment File Generation
```bash
# Generate development environment
node scripts/set-env.js development

# Generate staging environment with custom API URL
API_BASE_URL=https://custom-staging.com/api node scripts/set-env.js staging

# Generate production environment
API_BASE_URL=https://api.production.com node scripts/set-env.js production
```

## Docker and Container Deployment

When deploying in containers, you can set environment variables at runtime:

```bash
# Docker example
docker run -e API_BASE_URL=https://api.mycompany.com my-angular-app

# Kubernetes example
env:
  - name: API_BASE_URL
    value: "https://api.mycompany.com"
```

## CI/CD Integration

In your CI/CD pipeline, you can set environment-specific configurations:

```yaml
# Example GitHub Actions
- name: Build for production
  run: |
    export API_BASE_URL=${{ secrets.PRODUCTION_API_URL }}
    npm run build:prod

- name: Build for staging
  run: |
    export API_BASE_URL=${{ secrets.STAGING_API_URL }}
    npm run build:staging
```

## Troubleshooting

### Common Issues

1. **API calls failing**: Check that `API_BASE_URL` is correctly set and accessible
2. **CORS errors**: Ensure the backend API allows requests from your frontend domain
3. **Environment not updating**: Run the appropriate build script to regenerate environment files

### Debugging Environment Configuration

Check the browser's developer console for the current environment configuration:
```typescript
import { environment } from '../environments/environment';
console.log('Current environment:', environment);
```

### Verifying Environment Variables

The `set-env.js` script logs the configuration being used:
```bash
node scripts/set-env.js production
# Output:
# Environment configuration written to src/environments/environment.prod.ts
# Environment: production
# API Base URL: /api
# Production mode: true
# Using default API_BASE_URL for production environment
```