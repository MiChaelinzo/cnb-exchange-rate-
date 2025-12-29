import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExchangeRateService } from '../../services/exchange-rate.service';
import { ExchangeRate, ExchangeRateResponse } from '../../models/exchange-rate.interface';

@Component({
  selector: 'app-exchange-rate',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './exchange-rate.component.html',
  styleUrl: './exchange-rate.component.css'
})
export class ExchangeRateComponent implements OnInit {
  exchangeRates = signal<ExchangeRate[]>([]);
  lastUpdated = signal<string>('');
  sequenceNumber = signal<number>(0);
  isLoading = signal<boolean>(false);
  error = signal<string>('');

  constructor(private exchangeRateService: ExchangeRateService) {}

  ngOnInit(): void {
    this.loadExchangeRates();
  }

  loadExchangeRates(): void {
    this.isLoading.set(true);
    this.error.set('');

    this.exchangeRateService.getExchangeRates().subscribe({
      next: (response: ExchangeRateResponse) => {
        this.exchangeRates.set(response.rates);
        this.lastUpdated.set(response.date);
        this.sequenceNumber.set(response.sequenceNumber);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.error.set(error.message);
        this.isLoading.set(false);
      }
    });
  }

  retry(): void {
    this.loadExchangeRates();
  }
}