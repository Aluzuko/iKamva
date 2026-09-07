import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MonthlyPoint } from '../../models/admin.model';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bar-chart.html',
  styleUrl: './bar-chart.scss',
})
export class BarChart {
  @Input({ required: true }) points!: MonthlyPoint[];
  @Input() color = '#1b4b73';
  @Input() valuePrefix = '';

  get maxValue(): number {
    const max = Math.max(...this.points.map((p) => p.value), 1);
    return max;
  }

  heightPercent(value: number): number {
    return Math.max((value / this.maxValue) * 100, value > 0 ? 4 : 0);
  }
}
