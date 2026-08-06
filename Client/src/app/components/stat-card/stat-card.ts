import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export interface StatTrend {
  percent: number;
  label: string;
}

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-card.html',
  styleUrl: './stat-card.scss',
})
export class StatCard {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string;
  @Input() icon?: string;
  @Input() trend?: StatTrend;

  get trendPositive(): boolean {
    return (this.trend?.percent ?? 0) >= 0;
  }
}
