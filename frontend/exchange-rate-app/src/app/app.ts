import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ExchangeRateComponent } from './components/exchange-rate/exchange-rate.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ExchangeRateComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Exchange Rate Display');
}
