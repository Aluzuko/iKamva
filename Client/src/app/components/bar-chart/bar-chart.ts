import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MonthlyPoint } from '../../models/admin.model';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule],
  providers: [CurrencyPipe, DecimalPipe],
  templateUrl: './bar-chart.html',
  styleUrl: './bar-chart.scss',
})
export class BarChart {
  @Input({ required: true }) points: MonthlyPoint[] = [];
  @Input({ required: true }) label!: string;
  @Input() format: 'currency' | 'number' = 'number';
  @Input() highlightLast = true;

  constructor(private readonly currency: CurrencyPipe, private readonly decimal: DecimalPipe) {}

  get max(): number {
    return Math.max(0, ...this.points.map((p) => p.value));
  }

  height(value: number): number {
    return this.max ? Math.max(2, Math.round((value / this.max) * 100)) : 2;
  }

  display(value: number): string {
    return this.format === 'currency'
      ? this.currency.transform(value, 'ZAR', 'R', '1.0-0') ?? ''
      : this.decimal.transform(value, '1.0-0') ?? '';
  }

  get summary(): string {
    return `${this.label}: ` + this.points.map((p) => `${p.label} ${this.display(p.value)}`).join(', ');
  }
}
