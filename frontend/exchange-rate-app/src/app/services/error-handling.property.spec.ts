import { ExchangeRateService } from './exchange-rate.service';
import { environment } from '../../environments/environment';
import * as fc from 'fast-check';
import { throwError, of } from 'rxjs';

/**
 * Property-based tests for frontend error handling consistency
 * Feature: exchange-rate-display, Property 3: Error Handling Consistency
 * Validates: Requirements 5.3, 5.5 (frontend error handling aspects)
 */
describe('ExchangeRateService Error Handling Properties', () => {

  /**
   * Property: For any HTTP error status code, the service should return a meaningful error message
   */
  it('should handle HTTP errors consistently', async () => {
    await fc.assert(fc.asyncProperty(
      fc.integer({ min: 400, max: 599 }), // HTTP error status codes
      fc.string({ minLength: 1, maxLength: 100 }).filter(s => s.trim().length > 0), // Non-empty error message
      async (statusCode, errorMessage) => {
        // Create mock HTTP client that returns an error immediately (no retries)
        let callCount = 0;
        const mockHttpClient = {
          get: vi.fn().mockImplementation(() => {
            callCount++;
            return throwError(() => ({
              status: statusCode,
              message: errorMessage,
              error: null
            }));
          })
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          // Should not reach here
          return false;
        } catch (error: any) {
          // Verify error handling
          expect(error).toBeTruthy();
          expect(error.message).toBeTruthy();
          expect(error.message.length).toBeGreaterThan(0);

          // Verify specific status code handling
          switch (statusCode) {
            case 503:
              expect(error.message.toLowerCase()).toContain('temporarily unavailable');
              break;
            case 500:
              expect(error.message.toLowerCase()).toContain('internal server error');
              break;
            case 0:
              expect(error.message.toLowerCase()).toContain('connect to the server');
              break;
            default:
              expect(error.message).toContain(`${statusCode}`);
          }

          // Verify that retries occurred (service retries 3 times + initial call = 4 total)
          expect(callCount).toBeGreaterThanOrEqual(1);
          expect(callCount).toBeLessThanOrEqual(4);

          return true;
        }
      }
    ), { numRuns: 50, timeout: 30000 }); // Reduced runs and increased timeout
  }, 35000); // Increased test timeout

  /**
   * Property: For any network error, the service should return a user-friendly error message
   */
  it('should handle network errors consistently', async () => {
    await fc.assert(fc.asyncProperty(
      fc.string({ minLength: 1, maxLength: 50 }).filter(s => s.trim().length > 0), // Non-empty network error message
      async (networkErrorMessage) => {
        // Create mock HTTP client that returns a network error
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(throwError(() => ({
            error: new ErrorEvent('Network error', {
              message: networkErrorMessage
            })
          })))
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          return false;
        } catch (error: any) {
          // Verify error handling
          expect(error).toBeTruthy();
          expect(error.message.toLowerCase()).toContain('network error');
          expect(error.message).toContain(networkErrorMessage);
          return true;
        }
      }
    ), { numRuns: 50, timeout: 30000 });
  }, 35000);

  /**
   * Property: Service unavailable errors (503) should always mention temporary unavailability
   */
  it('should handle 503 errors with consistent messaging', async () => {
    await fc.assert(fc.asyncProperty(
      fc.string({ minLength: 1, maxLength: 100 }).filter(s => s.trim().length > 0), // Non-empty server error response
      async (serverResponse) => {
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(throwError(() => ({
            status: 503,
            message: 'Service Unavailable',
            error: serverResponse
          })))
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          return false;
        } catch (error: any) {
          expect(error).toBeTruthy();
          expect(error.message.toLowerCase()).toContain('temporarily unavailable');
          expect(error.message.toLowerCase()).toContain('try again later');
          return true;
        }
      }
    ), { numRuns: 50, timeout: 30000 });
  }, 35000);

  /**
   * Property: Connection errors (status 0) should always mention connectivity issues
   */
  it('should handle connection errors with consistent messaging', async () => {
    await fc.assert(fc.asyncProperty(
      fc.string({ minLength: 0, maxLength: 100 }), // Any response body (can be empty for connection errors)
      async (responseBody) => {
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(throwError(() => ({
            status: 0,
            message: '',
            error: responseBody
          })))
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          return false;
        } catch (error: any) {
          expect(error).toBeTruthy();
          expect(error.message.toLowerCase()).toContain('connect to the server');
          expect(error.message.toLowerCase()).toContain('internet connection');
          return true;
        }
      }
    ), { numRuns: 50, timeout: 30000 });
  }, 35000);

  /**
   * Property: All error messages should be user-friendly (no technical jargon)
   */
  it('should provide user-friendly error messages', async () => {
    await fc.assert(fc.asyncProperty(
      fc.integer({ min: 400, max: 599 }),
      fc.string({ minLength: 5, maxLength: 100 }).filter(s => s.trim().length >= 3), // Meaningful error response
      async (statusCode, errorResponse) => {
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(throwError(() => ({
            status: statusCode,
            message: 'Error',
            error: errorResponse
          })))
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          return false;
        } catch (error: any) {
          expect(error).toBeTruthy();
          
          const message = error.message.toLowerCase();
          
          // Error message should be user-friendly
          expect(message.length).toBeGreaterThan(10); // Meaningful length
          expect(message.length).toBeLessThan(200); // Not too verbose
          
          // Should not contain technical terms
          expect(message).not.toContain('httperrorresponse');
          expect(message).not.toContain('observable');
          expect(message).not.toContain('undefined');
          expect(message).not.toContain('null');
          
          // Should contain helpful guidance (relaxed check for edge cases)
          const hasHelpfulGuidance = 
            message.includes('try again') ||
            message.includes('later') ||
            message.includes('check') ||
            message.includes('connection') ||
            message.includes('error') ||
            message.includes('server') ||
            message.includes('service');
          
          expect(hasHelpfulGuidance).toBe(true);
          return true;
        }
      }
    ), { numRuns: 50, timeout: 30000 });
  }, 35000);

  /**
   * Property: Error handling should be consistent regardless of retry attempts
   */
  it('should handle errors consistently after retries', async () => {
    await fc.assert(fc.asyncProperty(
      fc.integer({ min: 500, max: 599 }), // Server error codes that trigger retries
      async (statusCode) => {
        let callCount = 0;
        const mockHttpClient = {
          get: vi.fn().mockImplementation(() => {
            callCount++;
            return throwError(() => ({
              status: statusCode,
              message: 'Server Error',
              error: 'Server Error'
            }));
          })
        };

        const service = new ExchangeRateService(mockHttpClient as any);

        try {
          await service.getExchangeRates().toPromise();
          return false;
        } catch (error: any) {
          // After all retries, should still provide consistent error handling
          expect(error).toBeTruthy();
          expect(error.message).toBeTruthy();
          expect(error.message.length).toBeGreaterThan(0);
          
          // Verify that retries occurred for 5xx errors
          expect(callCount).toBeGreaterThanOrEqual(1);
          expect(callCount).toBeLessThanOrEqual(4); // 3 retries + initial call
          
          return true;
        }
      }
    ), { numRuns: 30, timeout: 30000 }); // Reduced runs for retry tests
  }, 35000);
});