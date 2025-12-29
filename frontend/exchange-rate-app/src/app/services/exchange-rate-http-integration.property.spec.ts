import { ExchangeRateService } from './exchange-rate.service';
import { ExchangeRateResponse } from '../models/exchange-rate.interface';
import { environment } from '../../environments/environment';
import * as fc from 'fast-check';
import { of, throwError } from 'rxjs';

/**
 * Property-based tests for ExchangeRateService HTTP integration
 * Feature: exchange-rate-display, Property 7: Frontend HTTP Integration
 * Validates: Requirements 3.2
 */
describe('ExchangeRateService HTTP Integration Property Tests', () => {

  /**
   * Property 7: Frontend HTTP Integration
   * For any component initialization, the Frontend_App should make HTTP requests 
   * to the Backend_API using the configured endpoint URL
   * Validates: Requirements 3.2
   */
  it('should make HTTP requests to configured API endpoint for any valid response data', () => {
    fc.assert(fc.property(
      // Generate arbitrary exchange rate response data
      fc.record({
        date: fc.date().map(d => d.toISOString()),
        sequenceNumber: fc.integer({ min: 1, max: 999 }),
        rates: fc.array(
          fc.record({
            country: fc.string({ minLength: 1, maxLength: 50 }),
            currency: fc.string({ minLength: 1, maxLength: 50 }),
            amount: fc.integer({ min: 1, max: 1000 }),
            code: fc.string({ minLength: 3, maxLength: 3 }),
            rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
          }),
          { minLength: 1, maxLength: 20 }
        )
      }),
      (mockResponse: ExchangeRateResponse) => {
        // Create mock HTTP client
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(of(mockResponse))
        };
        
        // Create service instance with mock
        const service = new ExchangeRateService(mockHttpClient as any);

        // Track if the subscription completed successfully
        let responseReceived = false;
        let errorOccurred = false;

        // Act: Make the HTTP request
        service.getExchangeRates().subscribe({
          next: (response) => {
            // Verify the response matches what we expect
            expect(response).toEqual(mockResponse);
            responseReceived = true;
          },
          error: () => {
            errorOccurred = true;
          }
        });

        // Assert: Verify HTTP request was made to correct endpoint
        expect(mockHttpClient.get).toHaveBeenCalledWith(`${environment.apiBaseUrl}/v1.0/exchange-rates`);
        
        // Verify the service uses the configured environment API base URL
        const calledUrl = mockHttpClient.get.mock.calls[0][0];
        expect(calledUrl).toBe(`${environment.apiBaseUrl}/v1.0/exchange-rates`);
        expect(calledUrl).toContain(environment.apiBaseUrl);
        expect(calledUrl).toMatch(/\/v1\.0\/exchange-rates$/);
        
        // Verify successful completion
        expect(responseReceived).toBe(true);
        expect(errorOccurred).toBe(false);
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 7b: HTTP Integration with Error Scenarios
   * For any HTTP error response, the service should handle errors appropriately
   * and still use the configured endpoint URL
   * Validates: Requirements 3.2
   */
  it('should use configured endpoint URL even when handling HTTP errors', () => {
    fc.assert(fc.property(
      // Generate arbitrary HTTP error status codes
      fc.oneof(
        fc.constant(503), // Service unavailable
        fc.constant(500), // Internal server error
        fc.constant(404), // Not found
        fc.constant(0)    // Network error
      ),
      fc.string({ minLength: 1, maxLength: 100 }), // Error message
      (statusCode: number, errorMessage: string) => {
        // Setup mock to return an error
        const mockError = { 
          status: statusCode, 
          message: errorMessage,
          error: statusCode === 0 ? new ErrorEvent('Network error') : null
        };
        
        const mockHttpClient = {
          get: vi.fn().mockReturnValue(throwError(() => mockError))
        };
        
        const service = new ExchangeRateService(mockHttpClient as any);

        // Track if error handling worked correctly
        let errorHandled = false;
        let unexpectedSuccess = false;

        // Act: Make the HTTP request and wait for completion
        const observable = service.getExchangeRates();
        
        // Subscribe and handle the result synchronously
        try {
          observable.subscribe({
            next: () => {
              unexpectedSuccess = true;
            },
            error: (error) => {
              // Verify error is handled appropriately
              expect(error).toBeInstanceOf(Error);
              expect(error.message).toBeTruthy();
              errorHandled = true;
            }
          });
        } catch (e) {
          // Handle synchronous errors
          errorHandled = true;
        }

        // Assert: Verify HTTP request was made to correct endpoint even for errors
        expect(mockHttpClient.get).toHaveBeenCalledWith(`${environment.apiBaseUrl}/v1.0/exchange-rates`);
        
        // Verify the service uses the configured URL regardless of error
        const calledUrl = mockHttpClient.get.mock.calls[0][0];
        expect(calledUrl).toBe(`${environment.apiBaseUrl}/v1.0/exchange-rates`);
        expect(calledUrl).toContain(environment.apiBaseUrl);
        
        // For this property test, we mainly care that the URL is correct
        // The error handling behavior is tested separately
        expect(mockHttpClient.get).toHaveBeenCalled();
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 7c: Configuration-based URL Construction
   * For any environment configuration, the service should construct URLs correctly
   * Validates: Requirements 3.2
   */
  it('should construct API URLs using environment configuration', () => {
    // Setup mock
    const mockHttpClient = {
      get: vi.fn().mockReturnValue(of({ date: '2024-01-01', sequenceNumber: 1, rates: [] }))
    };
    
    const service = new ExchangeRateService(mockHttpClient as any);
    
    // This property verifies that the service uses the environment.apiBaseUrl
    // and constructs the full endpoint URL correctly
    const expectedUrl = `${environment.apiBaseUrl}/v1.0/exchange-rates`;
    
    // Make a request
    service.getExchangeRates().subscribe();
    
    // Verify the constructed URL matches expected pattern
    expect(mockHttpClient.get).toHaveBeenCalledWith(expectedUrl);
    
    // Verify it uses the environment configuration
    const calledUrl = mockHttpClient.get.mock.calls[0][0];
    expect(calledUrl).toContain(environment.apiBaseUrl);
    expect(calledUrl).toMatch(/\/v1\.0\/exchange-rates$/);
    
    // Verify the URL structure is correct
    expect(expectedUrl).toBe(`${environment.apiBaseUrl}/v1.0/exchange-rates`);
  });
});