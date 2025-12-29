const fs = require('fs');
const path = require('path');

// Get target environment from command line argument or default to 'development'
const targetEnv = process.argv[2] || 'development';

// Define environment configurations
const environments = {
  development: {
    production: false,
    apiBaseUrl: process.env.API_BASE_URL || 'http://localhost:5215/api'
  },
  staging: {
    production: false,
    apiBaseUrl: process.env.API_BASE_URL || 'https://staging-api.example.com/api'
  },
  production: {
    production: true,
    apiBaseUrl: process.env.API_BASE_URL || '/api'
  }
};

// Get configuration for target environment
const envConfig = environments[targetEnv];
if (!envConfig) {
  console.error(`Unknown environment: ${targetEnv}`);
  console.error(`Available environments: ${Object.keys(environments).join(', ')}`);
  process.exit(1);
}

// Determine target file path
const getTargetFile = (env) => {
  switch (env) {
    case 'production':
      return 'src/environments/environment.prod.ts';
    case 'staging':
      return 'src/environments/environment.staging.ts';
    default:
      return 'src/environments/environment.ts';
  }
};

const targetFile = getTargetFile(targetEnv);

// Generate environment file content
const envConfigContent = `// This file is generated automatically by scripts/set-env.js
// Do not edit manually - changes will be overwritten
// Environment: ${targetEnv}
// Generated at: ${new Date().toISOString()}

export const environment = {
  production: ${envConfig.production},
  apiBaseUrl: '${envConfig.apiBaseUrl}'
};
`;

// Write the environment file
const targetFilePath = path.resolve(__dirname, '..', targetFile);
fs.writeFileSync(targetFilePath, envConfigContent);

console.log(`Environment configuration written to ${targetFile}`);
console.log(`Environment: ${targetEnv}`);
console.log(`API Base URL: ${envConfig.apiBaseUrl}`);
console.log(`Production mode: ${envConfig.production}`);

// Log environment variable usage
if (process.env.API_BASE_URL) {
  console.log(`Using API_BASE_URL from environment: ${process.env.API_BASE_URL}`);
} else {
  console.log(`Using default API_BASE_URL for ${targetEnv} environment`);
}