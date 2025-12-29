import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeRateService } from '../../services/exchange-rate.service';
import { ExchangeRate, ExchangeRateResponse } from '../../models/exchange-rate.interface';

type SortField = 'country' | 'currency' | 'code' | 'rate';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-exchange-rate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './exchange-rate.component.html',
  styleUrl: './exchange-rate.component.css'
})
export class ExchangeRateComponent implements OnInit {
  allRates = signal<ExchangeRate[]>([]);
  searchTerm = signal<string>('');
  sortField = signal<SortField>('country');
  sortDirection = signal<SortDirection>('asc');
  
  lastUpdated = signal<string>('');
  sequenceNumber = signal<number>(0);
  isLoading = signal<boolean>(false);
  error = signal<string>('');

  // Computed signal for filtered and sorted exchange rates
  exchangeRates = computed(() => {
    let rates = this.allRates();
    
    // Apply search filter
    const search = this.searchTerm().toLowerCase().trim();
    if (search) {
      rates = rates.filter(rate => 
        rate.country.toLowerCase().includes(search) ||
        rate.currency.toLowerCase().includes(search) ||
        rate.code.toLowerCase().includes(search)
      );
    }

    // Apply sorting
    const field = this.sortField();
    const direction = this.sortDirection();
    
    return [...rates].sort((a, b) => {
      let comparison = 0;
      
      if (field === 'rate') {
        comparison = a.rate - b.rate;
      } else {
        const aValue = String(a[field]).toLowerCase();
        const bValue = String(b[field]).toLowerCase();
        comparison = aValue.localeCompare(bValue);
      }
      
      return direction === 'asc' ? comparison : -comparison;
    });
  });

  constructor(private exchangeRateService: ExchangeRateService) {}

  ngOnInit(): void {
    this.loadExchangeRates();
  }

  loadExchangeRates(): void {
    this.isLoading.set(true);
    this.error.set('');

    this.exchangeRateService.getExchangeRates().subscribe({
      next: (response: ExchangeRateResponse) => {
        this.allRates.set(response.rates);
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

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
  }

  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      // Toggle direction if clicking the same field
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      // Set new field with ascending direction
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  clearSearch(): void {
    this.searchTerm.set('');
  }
}