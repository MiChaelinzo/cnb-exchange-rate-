import { ExchangeRateComponent } from './exchange-rate.component';
import { ExchangeRateService } from '../../services/exchange-rate.service';
import { signal } from '@angular/core';

// Simple unit test without Angular TestBed
describe('ExchangeRateComponent', () => {
  let component: ExchangeRateComponent;
  let mockService: any;

  beforeEach(() => {
    mockService = {
      getExchangeRates: vi.fn()
    };
    component = new ExchangeRateComponent(mockService);
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.exchangeRates()).toEqual([]);
    expect(component.lastUpdated()).toBe('');
    expect(component.sequenceNumber()).toBe(0);
    expect(component.isLoading()).toBe(false);
    expect(component.error()).toBe('');
  });

  it('should have retry method', () => {
    expect(typeof component.retry).toBe('function');
  });
});