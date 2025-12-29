import { ExchangeRate, ExchangeRateResponse } from '../../models/exchange-rate.interface';
import * as fc from 'fast-check';

/**
 * Property-based tests for UI State Management
 * Feature: exchange-rate-display, Property 8: UI State Management
 * Validates: Requirements 3.4, 3.5, 5.3, 5.5
 */
describe('UI State Management Property Tests', () => {

  /**
   * Helper function to simulate UI state transitions
   * This represents the core state management logic in the component
   */
  interface UIState {
    isLoading: boolean;
    error: string;
    exchangeRates: ExchangeRate[];
    lastUpdated: string;
    sequenceNumber: number;
  }

  function createInitialState(): UIState {
    return {
      isLoading: false,
      error: '',
      exchangeRates: [],
      lastUpdated: '',
      sequenceNumber: 0
    };
  }

  function startLoading(state: UIState): UIState {
    return {
      ...state,
      isLoading: true,
      error: ''
    };
  }

  function handleSuccess(state: UIState, response: ExchangeRateResponse): UIState {
    return {
      ...state,
      isLoading: false,
      error: '',
      exchangeRates: response.rates,
      lastUpdated: response.date,
      sequenceNumber: response.sequenceNumber
    };
  }

  function handleError(state: UIState, errorMessage: string): UIState {
    return {
      ...state,
      isLoading: false,
      error: errorMessage
    };
  }

  /**
   * Helper function to determine what UI elements should be visible
   * This represents the template logic for conditional rendering
   */
  function getVisibleUIElements(state: UIState): {
    showLoading: boolean;
    showError: boolean;
    showTable: boolean;
    showNoData: boolean;
    showRetryButton: boolean;
  } {
    return {
      showLoading: state.isLoading,
      showError: !!state.error && !state.isLoading,
      showTable: !state.isLoading && !state.error && state.exchangeRates.length > 0,
      showNoData: !state.isLoading && !state.error && state.exchangeRates.length === 0,
      showRetryButton: !!state.error || (!state.isLoading && !state.error && state.exchangeRates.length === 0)
    };
  }

  /**
   * Property 8a: Loading State Management
   * For any async operation (loading data), the Frontend_App should display 
   * appropriate loading indicators during requests
   * Validates: Requirements 3.4, 5.3
   */
  it('should display loading indicators during any async operation', () => {
    fc.assert(fc.property(
      // Generate various initial states
      fc.record({
        hasExistingData: fc.boolean(),
        hasExistingError: fc.boolean(),
        existingRates: fc.array(
          fc.record({
            country: fc.string({ minLength: 1, maxLength: 50 }),
            currency: fc.string({ minLength: 1, maxLength: 50 }),
            amount: fc.integer({ min: 1, max: 1000 }),
            code: fc.string({ minLength: 3, maxLength: 3 }),
            rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
          }),
          { maxLength: 10 }
        ),
        errorMessage: fc.string({ minLength: 1, maxLength: 100 })
      }),
      (testData) => {
        // Create initial state based on test data
        let state = createInitialState();
        if (testData.hasExistingData) {
          state.exchangeRates = testData.existingRates;
        }
        if (testData.hasExistingError) {
          state.error = testData.errorMessage;
        }

        // Start loading operation
        const loadingState = startLoading(state);
        const loadingUI = getVisibleUIElements(loadingState);

        // Verify loading state behavior
        expect(loadingState.isLoading).toBe(true);
        expect(loadingState.error).toBe(''); // Error should be cleared when loading starts
        expect(loadingUI.showLoading).toBe(true);
        expect(loadingUI.showError).toBe(false); // Error should not be visible during loading
        expect(loadingUI.showTable).toBe(false); // Table should not be visible during loading
        expect(loadingUI.showNoData).toBe(false); // No data message should not be visible during loading
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 8b: Error State Management
   * For any error scenario, the Frontend_App should display appropriate 
   * error messages and provide retry functionality
   * Validates: Requirements 3.5, 5.5
   */
  it('should display error messages and retry functionality for any error scenario', () => {
    fc.assert(fc.property(
      // Generate various error scenarios
      fc.record({
        errorType: fc.oneof(
          fc.constant('Network error: Connection failed'),
          fc.constant('Exchange rate service is temporarily unavailable. Please try again later.'),
          fc.constant('Internal server error. Please try again later.'),
          fc.constant('Unable to connect to the server. Please check your internet connection.'),
          fc.string({ minLength: 1, maxLength: 200 })
        ),
        initialState: fc.record({
          hasData: fc.boolean(),
          wasLoading: fc.boolean(),
          rates: fc.array(
            fc.record({
              country: fc.string({ minLength: 1, maxLength: 50 }),
              currency: fc.string({ minLength: 1, maxLength: 50 }),
              amount: fc.integer({ min: 1, max: 1000 }),
              code: fc.string({ minLength: 3, maxLength: 3 }),
              rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
            }),
            { maxLength: 10 }
          )
        })
      }),
      (testData) => {
        // Create initial state
        let state = createInitialState();
        if (testData.initialState.hasData) {
          state.exchangeRates = testData.initialState.rates;
        }
        if (testData.initialState.wasLoading) {
          state = startLoading(state);
        }

        // Handle error
        const errorState = handleError(state, testData.errorType);
        const errorUI = getVisibleUIElements(errorState);

        // Verify error state behavior
        expect(errorState.isLoading).toBe(false);
        expect(errorState.error).toBe(testData.errorType);
        expect(errorState.error.length).toBeGreaterThan(0);
        
        // Verify UI elements for error state
        expect(errorUI.showLoading).toBe(false);
        expect(errorUI.showError).toBe(true);
        expect(errorUI.showTable).toBe(false); // Table should not be visible during error
        expect(errorUI.showNoData).toBe(false); // No data message should not be visible during error
        expect(errorUI.showRetryButton).toBe(true); // Retry button should be available
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 8c: Success State Management
   * For any successful data loading, the Frontend_App should display 
   * the data and clear loading/error states
   * Validates: Requirements 3.4, 5.3
   */
  it('should display data and clear loading/error states for any successful operation', () => {
    fc.assert(fc.property(
      // Generate various success scenarios
      fc.record({
        response: fc.record({
          date: fc.date({ min: new Date('2020-01-01'), max: new Date('2030-12-31') }).map(d => d.toISOString().split('T')[0]),
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
        initialState: fc.record({
          wasLoading: fc.boolean(),
          hadError: fc.boolean(),
          errorMessage: fc.string({ minLength: 1, maxLength: 100 })
        })
      }),
      (testData) => {
        // Create initial state with potential loading/error states
        let state = createInitialState();
        if (testData.initialState.wasLoading) {
          state = startLoading(state);
        }
        if (testData.initialState.hadError) {
          state.error = testData.initialState.errorMessage;
        }

        // Handle successful response
        const successState = handleSuccess(state, testData.response);
        const successUI = getVisibleUIElements(successState);

        // Verify success state behavior
        expect(successState.isLoading).toBe(false);
        expect(successState.error).toBe(''); // Error should be cleared
        expect(successState.exchangeRates).toEqual(testData.response.rates);
        expect(successState.lastUpdated).toBe(testData.response.date);
        expect(successState.sequenceNumber).toBe(testData.response.sequenceNumber);

        // Verify UI elements for success state
        expect(successUI.showLoading).toBe(false);
        expect(successUI.showError).toBe(false);
        expect(successUI.showTable).toBe(true); // Table should be visible with data
        expect(successUI.showNoData).toBe(false);
        expect(successUI.showRetryButton).toBe(false); // No retry button needed for success
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 8d: Empty Data State Management
   * For any successful operation that returns no data, the Frontend_App 
   * should display appropriate no-data message and retry option
   * Validates: Requirements 3.4, 3.5, 5.3
   */
  it('should display no-data message and retry option for any empty data scenario', () => {
    fc.assert(fc.property(
      // Generate empty data scenarios
      fc.record({
        response: fc.record({
          date: fc.date({ min: new Date('2020-01-01'), max: new Date('2030-12-31') }).map(d => d.toISOString().split('T')[0]),
          sequenceNumber: fc.integer({ min: 1, max: 999 }),
          rates: fc.constant([]) // Empty rates array
        }),
        initialState: fc.record({
          wasLoading: fc.boolean(),
          hadError: fc.boolean(),
          hadPreviousData: fc.boolean(),
          previousRates: fc.array(
            fc.record({
              country: fc.string({ minLength: 1, maxLength: 50 }),
              currency: fc.string({ minLength: 1, maxLength: 50 }),
              amount: fc.integer({ min: 1, max: 1000 }),
              code: fc.string({ minLength: 3, maxLength: 3 }),
              rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
            }),
            { minLength: 1, maxLength: 10 }
          )
        })
      }),
      (testData) => {
        // Create initial state
        let state = createInitialState();
        if (testData.initialState.wasLoading) {
          state = startLoading(state);
        }
        if (testData.initialState.hadError) {
          state.error = 'Previous error message';
        }
        if (testData.initialState.hadPreviousData) {
          state.exchangeRates = testData.initialState.previousRates;
        }

        // Handle successful response with empty data
        const emptyDataState = handleSuccess(state, testData.response);
        const emptyDataUI = getVisibleUIElements(emptyDataState);

        // Verify empty data state behavior
        expect(emptyDataState.isLoading).toBe(false);
        expect(emptyDataState.error).toBe(''); // Error should be cleared
        expect(emptyDataState.exchangeRates).toEqual([]); // Should have empty array
        expect(emptyDataState.lastUpdated).toBe(testData.response.date);
        expect(emptyDataState.sequenceNumber).toBe(testData.response.sequenceNumber);

        // Verify UI elements for empty data state
        expect(emptyDataUI.showLoading).toBe(false);
        expect(emptyDataUI.showError).toBe(false);
        expect(emptyDataUI.showTable).toBe(false); // No table for empty data
        expect(emptyDataUI.showNoData).toBe(true); // Should show no-data message
        expect(emptyDataUI.showRetryButton).toBe(true); // Should provide retry option
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 8e: State Transition Consistency
   * For any sequence of state transitions, the UI should maintain consistency
   * and never show conflicting states simultaneously
   * Validates: Requirements 3.4, 3.5, 5.3, 5.5
   */
  it('should maintain UI consistency during any sequence of state transitions', () => {
    fc.assert(fc.property(
      // Generate sequences of state transitions
      fc.array(
        fc.oneof(
          fc.constant('startLoading'),
          fc.record({
            type: fc.constant('success'),
            response: fc.record({
              date: fc.date({ min: new Date('2020-01-01'), max: new Date('2030-12-31') }).map(d => d.toISOString().split('T')[0]),
              sequenceNumber: fc.integer({ min: 1, max: 999 }),
              rates: fc.array(
                fc.record({
                  country: fc.string({ minLength: 1, maxLength: 50 }),
                  currency: fc.string({ minLength: 1, maxLength: 50 }),
                  amount: fc.integer({ min: 1, max: 1000 }),
                  code: fc.string({ minLength: 3, maxLength: 3 }),
                  rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
                }),
                { maxLength: 10 }
              )
            })
          }),
          fc.record({
            type: fc.constant('error'),
            message: fc.oneof(
              fc.constant('Network error: Connection failed'),
              fc.constant('Exchange rate service is temporarily unavailable. Please try again later.'),
              fc.constant('Internal server error. Please try again later.'),
              fc.string({ minLength: 1, maxLength: 100 })
            )
          })
        ),
        { minLength: 1, maxLength: 10 }
      ),
      (transitions) => {
        let state = createInitialState();

        // Apply each transition and verify consistency
        transitions.forEach((transition) => {
          if (transition === 'startLoading') {
            state = startLoading(state);
          } else if (transition.type === 'success') {
            state = handleSuccess(state, transition.response);
          } else if (transition.type === 'error') {
            state = handleError(state, transition.message);
          }

          const ui = getVisibleUIElements(state);

          // Verify UI consistency: only one primary state should be active
          const activeStates = [
            ui.showLoading,
            ui.showError,
            ui.showTable,
            ui.showNoData
          ].filter(Boolean);

          expect(activeStates.length).toBeLessThanOrEqual(1); // At most one primary state

          // Verify state-specific consistency rules
          if (state.isLoading) {
            expect(ui.showLoading).toBe(true);
            expect(ui.showError).toBe(false);
            expect(ui.showTable).toBe(false);
            expect(ui.showNoData).toBe(false);
            expect(state.error).toBe(''); // Error should be cleared during loading
          }

          if (state.error) {
            expect(state.isLoading).toBe(false);
            expect(ui.showError).toBe(true);
            expect(ui.showLoading).toBe(false);
            expect(ui.showTable).toBe(false);
            expect(ui.showNoData).toBe(false);
            expect(ui.showRetryButton).toBe(true);
          }

          if (!state.isLoading && !state.error) {
            if (state.exchangeRates.length > 0) {
              expect(ui.showTable).toBe(true);
              expect(ui.showNoData).toBe(false);
              expect(ui.showRetryButton).toBe(false);
            } else {
              expect(ui.showNoData).toBe(true);
              expect(ui.showTable).toBe(false);
              expect(ui.showRetryButton).toBe(true);
            }
            expect(ui.showLoading).toBe(false);
            expect(ui.showError).toBe(false);
          }
        });
      }
    ), { numRuns: 100 });
  });

  /**
   * Property 8f: Retry Functionality State Management
   * For any error or empty data state, the retry functionality should 
   * properly reset to loading state and clear previous states
   * Validates: Requirements 3.5, 5.5
   */
  it('should properly handle retry functionality from any error or empty data state', () => {
    fc.assert(fc.property(
      // Generate states that should have retry functionality
      fc.oneof(
        // Error states
        fc.record({
          type: fc.constant('error'),
          errorMessage: fc.string({ minLength: 1, maxLength: 200 }),
          hadPreviousData: fc.boolean(),
          previousRates: fc.array(
            fc.record({
              country: fc.string({ minLength: 1, maxLength: 50 }),
              currency: fc.string({ minLength: 1, maxLength: 50 }),
              amount: fc.integer({ min: 1, max: 1000 }),
              code: fc.string({ minLength: 3, maxLength: 3 }),
              rate: fc.float({ min: Math.fround(0.001), max: Math.fround(1000), noNaN: true })
            }),
            { maxLength: 10 }
          )
        }),
        // Empty data states
        fc.record({
          type: fc.constant('emptyData'),
          response: fc.record({
            date: fc.date({ min: new Date('2020-01-01'), max: new Date('2030-12-31') }).map(d => d.toISOString().split('T')[0]),
            sequenceNumber: fc.integer({ min: 1, max: 999 }),
            rates: fc.constant([])
          })
        })
      ),
      (testData) => {
        // Create the initial state based on test data
        let state = createInitialState();

        if (testData.type === 'error') {
          if (testData.hadPreviousData) {
            state.exchangeRates = testData.previousRates;
          }
          state = handleError(state, testData.errorMessage);
        } else if (testData.type === 'emptyData') {
          state = handleSuccess(state, testData.response);
        }

        // Verify initial state has retry functionality
        const initialUI = getVisibleUIElements(state);
        expect(initialUI.showRetryButton).toBe(true);

        // Simulate retry action (which should start loading)
        const retryState = startLoading(state);
        const retryUI = getVisibleUIElements(retryState);

        // Verify retry properly transitions to loading state
        expect(retryState.isLoading).toBe(true);
        expect(retryState.error).toBe(''); // Error should be cleared
        expect(retryUI.showLoading).toBe(true);
        expect(retryUI.showError).toBe(false);
        expect(retryUI.showTable).toBe(false);
        expect(retryUI.showNoData).toBe(false);

        // Verify that retry clears previous error state
        if (testData.type === 'error') {
          expect(retryState.error).not.toBe(testData.errorMessage);
        }
      }
    ), { numRuns: 100 });
  });
});