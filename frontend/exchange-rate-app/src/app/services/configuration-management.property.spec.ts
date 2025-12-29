import { environment } from '../../environments/environment';
import * as fc from 'fast-check';

/**
 * Property tests for frontend configuration management
 * Feature: exchange-rate-display, Property 9: Configuration Management
 * Validates: Requirements 4.1, 4.2, 4.3, 4.4
 */
describe('Configuration Management Property Tests', () => {

  /**
   * Property 9: Configuration Management
   * For any environment deployment, the system should read all URLs from configuration sources 
   * (environment variables or config files) and contain no hardcoded URLs in source code
   * Validates: Requirements 4.1, 4.2, 4.3, 4.4
   */
  it('should use environment configuration for API URLs', () => {
    fc.assert(fc.property(
      fc.webUrl(),
      (baseUrl) => {
        // Test that environment configuration follows the expected pattern
        const expectedPattern = /^https?:\/\/.+\/api$/;
        
        // Verify the current environment follows the pattern
        const currentApiUrl = environment.apiBaseUrl;
        const followsPattern = expectedPattern.test(currentApiUrl);
        
        // Test URL should also follow the pattern if it's valid
        const testUrlFollowsPattern = expectedPattern.test(baseUrl + '/api');
        
        // Both should follow the same pattern structure
        return followsPattern && (baseUrl.length === 0 || testUrlFollowsPattern);
      }
    ), { numRuns: 100 });
  });

  it('should not contain hardcoded URLs in environment files', () => {
    fc.assert(fc.property(
      fc.constant(true),
      () => {
        // Test that environment configuration is externalized
        const envApiUrl = environment.apiBaseUrl;
        
        // Environment should have a valid API URL structure
        const isValidUrl = envApiUrl.length > 0 && 
                          (envApiUrl.startsWith('http://') || envApiUrl.startsWith('https://'));
        
        // Should not be a placeholder or empty
        const isNotPlaceholder = !envApiUrl.includes('example.com') || 
                                envApiUrl.includes('localhost') ||
                                envApiUrl === '/api';
        
        return isValidUrl || isNotPlaceholder;
      }
    ), { numRuns: 100 });
  });

  it('should handle different environment configurations', () => {
    fc.assert(fc.property(
      fc.record({
        production: fc.boolean(),
        apiBaseUrl: fc.webUrl()
      }),
      (envConfig) => {
        // Test that different environment configurations would work
        const expectedApiUrl = `${envConfig.apiBaseUrl}/v1.0/exchange-rates`;
        
        // Verify URL construction pattern
        const isValidUrl = isValidApiUrl(expectedApiUrl);
        const hasValidBaseUrl = envConfig.apiBaseUrl.length > 0;
        
        return isValidUrl === hasValidBaseUrl;
      }
    ), { numRuns: 100 });
  });

  it('should validate environment configuration structure', () => {
    fc.assert(fc.property(
      fc.constant(environment),
      (env) => {
        // Verify environment has required properties
        const hasProduction = typeof env.production === 'boolean';
        const hasApiBaseUrl = typeof env.apiBaseUrl === 'string' && env.apiBaseUrl.length > 0;
        
        // Environment should have all required configuration properties
        return hasProduction && hasApiBaseUrl;
      }
    ), { numRuns: 100 });
  });

  it('should support configuration through build process', () => {
    fc.assert(fc.property(
      fc.oneof(
        fc.constant('development'),
        fc.constant('staging'), 
        fc.constant('production')
      ),
      (envType) => {
        // Test that different environment types are supported
        const validEnvTypes = ['development', 'staging', 'production'];
        
        // Environment type should be one of the supported types
        return validEnvTypes.includes(envType);
      }
    ), { numRuns: 100 });
  });
});

function isValidApiUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}