import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, retry, tap, shareReplay } from 'rxjs/operators';
import { ExchangeRateResponse } from '../models/exchange-rate.interface';
import { environment } from '../../environments/environment';

interface CacheEntry {
  data: ExchangeRateResponse;
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class ExchangeRateService {
  private readonly apiUrl = `${environment.apiBaseUrl}/v1.0/exchange-rates`;
  private readonly CACHE_DURATION = 5 * 60 * 1000; // 5 minutes in milliseconds
  private cache: CacheEntry | null = null;
  private pendingRequest: Observable<ExchangeRateResponse> | null = null;

  constructor(private http: HttpClient) {}

  getExchangeRates(): Observable<ExchangeRateResponse> {
    // Check if cache is valid
    if (this.cache && Date.now() - this.cache.timestamp < this.CACHE_DURATION) {
      if (!environment.production) {
        console.log('Returning cached exchange rates');
      }
      return of(this.cache.data);
    }

    // If there's already a pending request, return it to avoid duplicate requests
    if (this.pendingRequest) {
      if (!environment.production) {
        console.log('Returning pending request for exchange rates');
      }
      return this.pendingRequest;
    }

    if (!environment.production) {
      console.log('Fetching fresh exchange rates from API');
    }
    
    this.pendingRequest = this.http.get<ExchangeRateResponse>(this.apiUrl)
      .pipe(
        retry({
          count: 3,
          delay: 1000
        }),
        tap(response => {
          // Update cache
          this.cache = {
            data: response,
            timestamp: Date.now()
          };
          if (!environment.production) {
            console.log('Exchange rates cached successfully');
          }
        }),
        catchError(this.handleError.bind(this)),
        shareReplay(1), // Share the result with multiple subscribers
        tap({
          complete: () => {
            // Clear pending request when complete
            this.pendingRequest = null;
          },
          error: () => {
            // Clear pending request on error
            this.pendingRequest = null;
          }
        })
      );

    return this.pendingRequest;
  }

  /**
   * Clear the cache to force a fresh API call
   */
  clearCache(): void {
    this.cache = null;
    if (!environment.production) {
      console.log('Exchange rates cache cleared');
    }
  }

  /**
   * Check if cached data is available
   */
  isCacheValid(): boolean {
    return this.cache !== null && Date.now() - this.cache.timestamp < this.CACHE_DURATION;
  }

  /**
   * Get cache age in seconds
   */
  getCacheAge(): number | null {
    if (!this.cache) {
      return null;
    }
    return Math.floor((Date.now() - this.cache.timestamp) / 1000);
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Network error: ${error.error.message}`;
    } else {
      // Server-side error
      switch (error.status) {
        case 503:
          errorMessage = 'Exchange rate service is temporarily unavailable. Please try again later.';
          break;
        case 500:
          errorMessage = 'Internal server error. Please try again later.';
          break;
        case 0:
          errorMessage = 'Unable to connect to the server. Please check your internet connection.';
          break;
        default:
          errorMessage = `Server returned code ${error.status}: ${error.message}`;
      }
    }
    
    if (!environment.production) {
      console.error('ExchangeRateService error:', error);
    }
    return throwError(() => new Error(errorMessage));
  }
}