import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export interface StatTrend {
  percent: number;
  label: string;
}

export type StatTone = 'slate' | 'teal' | 'yellow' | 'mint' | 'rose';

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
  @Input() hint?: string;
  @Input() tone: StatTone = 'slate';

  get trendPositive(): boolean {
    return (this.trend?.percent ?? 0) >= 0;
  }
}
