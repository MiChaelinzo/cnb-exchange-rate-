export interface ExchangeRate {
  country: string;
  currency: string;
  amount: number;
  code: string;
  rate: number;
}

export interface ExchangeRateResponse {
  date: string;
  sequenceNumber: number;
  rates: ExchangeRate[];
}