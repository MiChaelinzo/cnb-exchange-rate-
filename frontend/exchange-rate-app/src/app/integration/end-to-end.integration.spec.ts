/**
 * End-to-End Integration Tests for Exchange Rate Application
 * Tests complete data flow from service to component display
 * Verifies error handling across the entire frontend stack
 * **Validates: All frontend requirements**
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ExchangeRateComponent } from '../components/exchange-rate/exchange-rate.component';
import { ExchangeRateService } from '../services/exchange-rate.service';
import { ExchangeRateResponse } from '../models/exchange-rate.interface';

describe('End-to-End Integration Tests', () => {
  let component: ExchangeRateComponent;
  let fixture: ComponentFixture<ExchangeRateComponent>;
  let service: ExchangeRateService;
  let httpMock: HttpTestingController;

  const mockExchangeRateResponse: ExchangeRateResponse = {
    date: new Date('2024-01-03'),
    sequenceNumber: 1,
    rates: [
      {
        country: 'Australia',
        currency: 'dollar',
        amount: 1,
        code: 'AUD',
        rate: 23.282
      },
      {
        country: 'USA',
        currency: 'dollar',
        amount: 1,
        code: 'USD',
        rate: 25.347
      },
      {
        country: 'United Kingdom',
        currency: 'pound',
        amount: 1,
        code: 'GBP',
        rate: 28.456
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExchangeRateComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ExchangeRateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExchangeRateComponent);
    component = fixture.componentInstance;
    service = TestBed.inject(ExchangeRateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should complete end-to-end data flow from service to display', async () => {
    // Test complete data flow: HTTP request -> Service -> Component -> DOM
    
    // Act - Initialize component (triggers ngOnInit)
    fixture.detectChanges();

    // Verify HTTP request was made
    const req = httpMock.expectOne(request => 
      request.url.includes('/api/v1/exchange-rates') && request.method === 'GET'
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('Accept')).toBe('application/json');

    // Respond with mock data
    req.flush(mockExchangeRateResponse);
    
    // Wait for async operations to complete
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert - Verify data flows through to component
    expect(component.exchangeRates()).toEqual(mockExchangeRateResponse.rates);
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe(null);

    // Assert - Verify data is displayed in DOM
    const compiled = fixture.nativeElement as HTMLElement;
    const tableRows = compiled.querySelectorAll('tbody tr');
    
    expect(tableRows.length).toBe(3);
    
    // Verify first row data
    const firstRowCells = tableRows[0].querySelectorAll('td');
    expect(firstRowCells[0].textContent?.trim()).toBe('Australia');
    expect(firstRowCells[1].textContent?.trim()).toBe('dollar');
    expect(firstRowCells[2].textContent?.trim()).toBe('1');
    expect(firstRowCells[3].textContent?.trim()).toBe('AUD');
    expect(firstRowCells[4].textContent?.trim()).toBe('23.282');

    // Verify table headers are present
    const headers = compiled.querySelectorAll('th');
    expect(headers.length).toBe(5);
    expect(headers[0].textContent?.trim()).toBe('Country');
    expect(headers[1].textContent?.trim()).toBe('Currency');
    expect(headers[2].textContent?.trim()).toBe('Amount');
    expect(headers[3].textContent?.trim()).toBe('Code');
    expect(headers[4].textContent?.trim()).toBe('Rate');
  });

  it('should handle loading states throughout the data flow', async () => {
    // Test UI state management during async operations
    
    // Act - Initialize component
    fixture.detectChanges();

    // Assert - Verify loading state is shown initially
    expect(component.loading()).toBe(true);
    expect(component.error()).toBe(null);
    
    const compiled = fixture.nativeElement as HTMLElement;
    const loadingElement = compiled.querySelector('.loading');
    expect(loadingElement).toBeTruthy();
    expect(loadingElement?.textContent?.trim()).toBe('Loading exchange rates...');

    // Complete the request
    const req = httpMock.expectOne(request => request.url.includes('/api/v1/exchange-rates'));
    req.flush(mockExchangeRateResponse);
    
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert - Verify loading state is cleared
    expect(component.loading()).toBe(false);
    const loadingElementAfter = compiled.querySelector('.loading');
    expect(loadingElementAfter).toBeFalsy();
  });

  it('should handle error states throughout the data flow', async () => {
    // Test error handling across the entire frontend stack
    
    // Act - Initialize component
    fixture.detectChanges();

    // Simulate network error
    const req = httpMock.expectOne(request => request.url.includes('/api/v1/exchange-rates'));
    req.flush('Network Error', { status: 503, statusText: 'Service Unavailable' });
    
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert - Verify error state is handled
    expect(component.loading()).toBe(false);
    expect(component.error()).toBeTruthy();
    expect(component.exchangeRates()).toEqual([]);

    // Verify error is displayed in DOM
    const compiled = fixture.nativeElement as HTMLElement;
    const errorElement = compiled.querySelector('.error');
    expect(errorElement).toBeTruthy();
    expect(errorElement?.textContent).toContain('Failed to load exchange rates');
    
    // Verify retry button is present
    const retryButton = compiled.querySelector('.retry-button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();
    expect(retryButton.textContent?.trim()).toBe('Retry');
  });

  it('should handle retry functionality in error scenarios', async () => {
    // Test error recovery and retry functionality
    
    // Act - Initialize component and simulate error
    fixture.detectChanges();
    
    const req1 = httpMock.expectOne(request => request.url.includes('/api/v1/exchange-rates'));
    req1.flush('Network Error', { status: 503, statusText: 'Service Unavailable' });
    
    await fixture.whenStable();
    fixture.detectChanges();

    // Verify error state
    expect(component.error()).toBeTruthy();
    
    // Act - Click retry button
    const compiled = fixture.nativeElement as HTMLElement;
    const retryButton = compiled.querySelector('.retry-button') as HTMLButtonElement;
    retryButton.click();
    
    fixture.detectChanges();

    // Verify loading state is shown again
    expect(component.loading()).toBe(true);
    expect(component.error()).toBe(null);

    // Complete the retry request successfully
    const req2 = httpMock.expectOne(request => request.url.includes('/api/v1/exchange-rates'));
    req2.flush(mockExchangeRateResponse);
    
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert - Verify successful recovery
    expect(component.loading()).toBe(false);
    expect(component.error()).toBe(null);
    expect(component.exchangeRates()).toEqual(mockExchangeRateResponse.rates);
  });

  it('should validate configuration management integration', async () => {
    // Test configuration management integration
    
    // Verify service uses correct API endpoint from configuration
    fixture.detectChanges();
    
    const req = httpMock.expectOne(request => {
      // Verify the request URL includes the expected API path
      expect(request.url).toMatch(/\/api\/v1\/exchange-rates$/);
      return true;
    });
    
    // Verify request headers
    expect(req.request.headers.get('Accept')).toBe('application/json');
    
    req.flush(mockExchangeRateResponse);
    await fixture.whenStable();
  });

  it('should maintain responsive design structure', async () => {
    // Test responsive design functionality
    
    // Act - Load data
    fixture.detectChanges();
    
    const req = httpMock.expectOne(request => request.url.includes('/api/v1/exchange-rates'));
    req.flush(mockExchangeRateResponse);
    
    await fixture.whenStable();
    fixture.detectChanges();

    // Assert - Verify table structure supports responsive design
    const compiled = fixture.nativeElement as HTMLElement;
    const table = compiled.querySelector('table');
    expect(table).toBeTruthy();
    
    // Verify all required columns are present for mobile compatibility
    const headers = compiled.querySelectorAll('th');
    expect(headers.length).toBe(5);
    
    const rows = compiled.querySelectorAll('tbody tr');
    expect(rows.length).toBe(3);
    
    // Verify each row has all required cells
    rows.forEach(row => {
      const cells = row.querySelectorAll('td');
      expect(cells.length).toBe(5);
    });
  });
});