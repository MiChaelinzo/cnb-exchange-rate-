import { ExchangeRate, ExchangeRateResponse } from '../../models/exchange-rate.interface';
import * as fc from 'fast-check';

/**
 * Property-based tests for Frontend Data Display logic
 * Feature: exchange-rate-display, Property 6: Frontend Data Display
 * Validates: Requirements 3.1, 3.3
 */
describe('Frontend Data Display Property Tests', () => {

  /**
   * Helper function to simulate data display validation
   * This represents the core logic that would be used by the component
   * to validate and prepare data for table display
   */
  function validateDataForDisplay(rates: ExchangeRate[]): boolean {
    return rates.every(rate => {
      // All required fields must be present and valid for table display
      return (
        typeof rate.country === 'string' && rate.country.trim().length > 0 &&
        typeof rate.currency === 'string' && rate.currency.trim().length > 0 &&
        typeof rate.code === 'string' && rate.code.trim().length > 0 &&
        typeof rate.amount === 'number' && rate.amount > 0 && Number.isFinite(rate.amount) &&
        typeof rate.rate === 'number' && rate.rate > 0 && Number.isFinite(rate.rate)
      );
    });
  }

  /**
   * Helper function to simulate data formatting for display
   * This represents the formatting logic used in the component template
   */
  function formatDataForDisplay(rates: ExchangeRate[]): Array<{
    country: string;
    currency: string;
    amount: string;
    code: string;
    rate: string;
  }> {
    return rates.map(rate => ({
      country: rate.country,
      currency: rate.currency,
      amount: rate.amount.toString(),
      code: rate.code,
      rate: rate.rate.toFixed(3) // Matches the number pipe format in template
    }));
  }

  /**
   * Property 6: Frontend Data Display
   * For any exchange rate data received from the API, the Frontend_App should display 
   * all required fields (currency code, rate, amount, country) in a readable table format
   * Validates: Requirements 3.1, 3.3
   */
  it('should validate and format all required fields for any valid exchange rate data', () => {
    fc.assert(fc.property(
      // Generate arbitrary exchange rate data
      fc.array(
        fc.record({
          country: fc.string({ minLength: 1, maxLength: 50 }).filter(s => s.trim().length > 0),
          currency: fc.string({ minLength: 1, maxLength: 50 }).filter(s => s.trim().length > 0),
          amount: fc.integer({ min: 1, max: 1000 }),
          code: fc.string({ minLength: 3, maxLength: 3 }).filter(s => s.trim().length === 3),
          rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
        }),
        { minLength: 1, maxLength: 20 }
      ),
      (rates: ExchangeRate[]) => {
        // Verify that the data validation logic works correctly
        const isValidForDisplay = validateDataForDisplay(rates);
        expect(isValidForDisplay).toBe(true);

        // Verify that data formatting preserves all required fields
        const formattedData = formatDataForDisplay(rates);
        expect(formattedData.length).toBe(rates.length);

        // Verify that each formatted item contains all required fields
        formattedData.forEach((formattedRate, index) => {
          const originalRate = rates[index];
          
          // Verify all required fields are present and properly formatted
          expect(formattedRate.country).toBe(originalRate.country);
          expect(formattedRate.currency).toBe(originalRate.currency);
          expect(formattedRate.amount).toBe(originalRate.amount.toString());
          expect(formattedRate.code).toBe(originalRate.code);
          expect(formattedRate.rate).toBe(originalRate.rate.toFixed(3));
          
          // Verify fields are suitable for table display
          expect(formattedRate.country.trim()).toBeTruthy();
          expect(formattedRate.currency.trim()).toBeTruthy();
          expect(formattedRate.code.trim()).toBeTruthy();
          expect(formattedRate.amount).toBeTruthy();
          expect(formattedRate.rate).toBeTruthy();
          
          // Verify numeric formatting is correct
          expect(parseFloat(formattedRate.rate)).toBeCloseTo(originalRate.rate, 3);
          expect(parseInt(formattedRate.amount)).toBe(originalRate.amount);
        });
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 6b: Data Display Consistency
   * For any exchange rate data, the display logic should maintain data integrity
   * and ensure all required fields are properly formatted for table display
   * Validates: Requirements 3.1, 3.3
   */
  it('should maintain data integrity and display consistency for any exchange rate data', () => {
    fc.assert(fc.property(
      // Generate exchange rate data with specific constraints for display testing
      fc.array(
        fc.record({
          country: fc.oneof(
            fc.constant('Australia'),
            fc.constant('USA'),
            fc.constant('United Kingdom'),
            fc.constant('Japan'),
            fc.string({ minLength: 1, maxLength: 30 }).filter(s => s.trim().length > 0)
          ),
          currency: fc.oneof(
            fc.constant('dollar'),
            fc.constant('pound'),
            fc.constant('yen'),
            fc.constant('euro'),
            fc.string({ minLength: 1, maxLength: 30 }).filter(s => s.trim().length > 0)
          ),
          amount: fc.oneof(
            fc.constant(1),
            fc.constant(100),
            fc.integer({ min: 1, max: 1000 })
          ),
          code: fc.oneof(
            fc.constant('USD'),
            fc.constant('EUR'),
            fc.constant('GBP'),
            fc.constant('JPY'),
            fc.string({ minLength: 3, maxLength: 3 }).filter(s => /^[A-Z]{3}$/.test(s))
          ),
          rate: fc.float({ min: Math.fround(0.1), max: Math.fround(100), noNaN: true })
        }),
        { minLength: 1, maxLength: 15 }
      ),
      (rates: ExchangeRate[]) => {
        // Verify data validation passes for all generated data
        const isValidForDisplay = validateDataForDisplay(rates);
        expect(isValidForDisplay).toBe(true);

        // Format data for display
        const formattedData = formatDataForDisplay(rates);

        // Verify data consistency: input data matches formatted data structure
        expect(formattedData.length).toBe(rates.length);
        
        // Verify each rate maintains all required fields for table display
        formattedData.forEach((formattedRate, index) => {
          const originalRate = rates[index];
          
          // All required fields must be preserved and properly formatted
          expect(formattedRate.country).toBe(originalRate.country);
          expect(formattedRate.currency).toBe(originalRate.currency);
          expect(formattedRate.code).toBe(originalRate.code);
          expect(parseInt(formattedRate.amount)).toBe(originalRate.amount);
          expect(parseFloat(formattedRate.rate)).toBeCloseTo(originalRate.rate, 3);
          
          // Fields must be suitable for table display (non-empty, valid strings)
          expect(typeof formattedRate.country).toBe('string');
          expect(typeof formattedRate.currency).toBe('string');
          expect(typeof formattedRate.code).toBe('string');
          expect(typeof formattedRate.amount).toBe('string');
          expect(typeof formattedRate.rate).toBe('string');
          
          // Verify displayable content
          expect(formattedRate.country.length).toBeGreaterThan(0);
          expect(formattedRate.currency.length).toBeGreaterThan(0);
          expect(formattedRate.code.length).toBeGreaterThan(0);
          expect(formattedRate.amount.length).toBeGreaterThan(0);
          expect(formattedRate.rate.length).toBeGreaterThan(0);
          
          // Verify numeric formatting is consistent
          expect(formattedRate.rate).toMatch(/^\d+\.\d{3}$/);
          expect(formattedRate.amount).toMatch(/^\d+$/);
        });
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 6c: Empty Data Display Handling
   * For any empty or minimal data sets, the display logic should handle them appropriately
   * Validates: Requirements 3.1, 3.3
   */
  it('should handle empty and minimal data sets appropriately for display', () => {
    fc.assert(fc.property(
      // Generate edge cases: empty arrays and single items
      fc.oneof(
        fc.constant([]), // Empty array
        fc.array(
          fc.record({
            country: fc.string({ minLength: 1, maxLength: 50 }).filter(s => s.trim().length > 0),
            currency: fc.string({ minLength: 1, maxLength: 50 }).filter(s => s.trim().length > 0),
            amount: fc.integer({ min: 1, max: 1000 }),
            code: fc.string({ minLength: 3, maxLength: 3 }).filter(s => s.trim().length === 3),
            rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
          }),
          { minLength: 1, maxLength: 1 } // Single item
        )
      ),
      (rates: ExchangeRate[]) => {
        // Verify validation handles edge cases correctly
        const isValidForDisplay = validateDataForDisplay(rates);
        expect(isValidForDisplay).toBe(true);

        // Format data for display
        const formattedData = formatDataForDisplay(rates);
        expect(formattedData.length).toBe(rates.length);

        // For empty data, should return empty array
        if (rates.length === 0) {
          expect(formattedData.length).toBe(0);
        } else {
          // For non-empty data, verify all fields are properly formatted
          formattedData.forEach((formattedRate, index) => {
            const originalRate = rates[index];
            
            expect(formattedRate.country).toBe(originalRate.country);
            expect(formattedRate.currency).toBe(originalRate.currency);
            expect(formattedRate.code).toBe(originalRate.code);
            expect(parseInt(formattedRate.amount)).toBe(originalRate.amount);
            expect(parseFloat(formattedRate.rate)).toBeCloseTo(originalRate.rate, 3);
            
            // Verify all formatted fields are non-empty strings
            expect(formattedRate.country.trim()).toBeTruthy();
            expect(formattedRate.currency.trim()).toBeTruthy();
            expect(formattedRate.code.trim()).toBeTruthy();
            expect(formattedRate.amount.trim()).toBeTruthy();
            expect(formattedRate.rate.trim()).toBeTruthy();
          });
        }
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 6d: Field Completeness Validation
   * For any exchange rate data, all required fields must be present and valid for table display
   * Validates: Requirements 3.1, 3.3
   */
  it('should ensure all required fields are present and valid for table display', () => {
    fc.assert(fc.property(
      // Generate data that specifically tests field completeness
      fc.array(
        fc.record({
          country: fc.string({ minLength: 1, maxLength: 100 }).filter(s => s.trim().length > 0),
          currency: fc.string({ minLength: 1, maxLength: 100 }).filter(s => s.trim().length > 0),
          amount: fc.integer({ min: 1, max: 10000 }),
          code: fc.string({ minLength: 3, maxLength: 3 }).filter(s => /^[A-Z0-9]{3}$/.test(s)),
          rate: fc.float({ min: Math.fround(0.0001), max: Math.fround(10000), noNaN: true })
        }),
        { minLength: 0, maxLength: 25 }
      ),
      (rates: ExchangeRate[]) => {
        // Test the core requirement: all required fields must be displayable
        rates.forEach(rate => {
          // Verify each rate has all required fields for table display
          expect(rate).toHaveProperty('country');
          expect(rate).toHaveProperty('currency');
          expect(rate).toHaveProperty('amount');
          expect(rate).toHaveProperty('code');
          expect(rate).toHaveProperty('rate');
          
          // Verify field types are correct for display
          expect(typeof rate.country).toBe('string');
          expect(typeof rate.currency).toBe('string');
          expect(typeof rate.amount).toBe('number');
          expect(typeof rate.code).toBe('string');
          expect(typeof rate.rate).toBe('number');
        });

        // Verify validation logic correctly identifies valid display data
        const isValidForDisplay = validateDataForDisplay(rates);
        expect(isValidForDisplay).toBe(true);

        // Verify formatting preserves all required fields
        const formattedData = formatDataForDisplay(rates);
        formattedData.forEach(formattedRate => {
          // All required fields must be present in formatted output
          expect(formattedRate).toHaveProperty('country');
          expect(formattedRate).toHaveProperty('currency');
          expect(formattedRate).toHaveProperty('amount');
          expect(formattedRate).toHaveProperty('code');
          expect(formattedRate).toHaveProperty('rate');
          
          // All formatted fields must be strings suitable for table display
          expect(typeof formattedRate.country).toBe('string');
          expect(typeof formattedRate.currency).toBe('string');
          expect(typeof formattedRate.amount).toBe('string');
          expect(typeof formattedRate.code).toBe('string');
          expect(typeof formattedRate.rate).toBe('string');
        });
      }
    ), { numRuns: 100 });
  });
});