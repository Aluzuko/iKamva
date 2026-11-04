import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { Analytics } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { PageHeader } from '../../components/page-header/page-header';
import { StatCard } from '../../components/stat-card/stat-card';
import { BarChart } from '../../components/bar-chart/bar-chart';

interface MonthRow {
  label: string;
  revenue: number;
  bookings: number;
  cancellations: number;
  cancelRate: number;
}

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, PageHeader, StatCard, BarChart],
  templateUrl: './admin-analytics.html',
})
export class AdminAnalytics implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly data = signal<Analytics | null>(null);

  readonly rows = computed<MonthRow[]>(() => {
    const d = this.data();
    if (!d) {
      return [];
    }
    return d.revenueByMonth.map((r, i) => {
      const bookings = d.bookingsByMonth[i]?.value ?? 0;
      const cancellations = d.cancellationsByMonth[i]?.value ?? 0;
      const total = bookings + cancellations;
      return {
        label: r.label,
        revenue: r.value,
        bookings,
        cancellations,
        cancelRate: total ? Math.round((cancellations / total) * 100) : 0,
      };
    });
  });

  readonly totals = computed(() => {
    const rows = this.rows();
    const revenue = rows.reduce((t, r) => t + r.revenue, 0);
    const bookings = rows.reduce((t, r) => t + r.bookings, 0);
    const cancellations = rows.reduce((t, r) => t + r.cancellations, 0);
    return {
      revenue,
      bookings,
      cancellations,
      average: bookings ? revenue / bookings : 0,
      cancelRate: bookings + cancellations ? Math.round((cancellations / (bookings + cancellations)) * 100) : 0,
    };
  });

  readonly hasActivity = computed(() => this.rows().some((r) => r.revenue || r.bookings || r.cancellations));

  constructor(private readonly adminApi: Admin) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.adminApi.getAnalytics().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  exportCsv(): void {
    const header = 'Month,Revenue,Bookings,Cancellations,Cancellation rate %';
    const lines = this.rows().map((r) => [r.label, r.revenue.toFixed(2), r.bookings, r.cancellations, r.cancelRate].join(','));
    const blob = new Blob([[header, ...lines].join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'ikamva-monthly-report.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
