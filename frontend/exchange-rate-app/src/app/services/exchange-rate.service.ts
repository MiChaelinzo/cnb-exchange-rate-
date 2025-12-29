import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, retry } from 'rxjs/operators';
import { ExchangeRateResponse } from '../models/exchange-rate.interface';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ExchangeRateService {
  private readonly apiUrl = `${environment.apiBaseUrl}/v1.0/exchange-rates`;

  constructor(private http: HttpClient) {}

  getExchangeRates(): Observable<ExchangeRateResponse> {
    return this.http.get<ExchangeRateResponse>(this.apiUrl)
      .pipe(
        retry({
          count: 3,
          delay: 1000
        }),
        catchError(this.handleError)
      );
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
    
    console.error('ExchangeRateService error:', error);
    return throwError(() => new Error(errorMessage));
  }
}