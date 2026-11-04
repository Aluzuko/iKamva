import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminDashboard as AdminDashboardData, RecentBooking } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { StatCard } from '../../components/stat-card/stat-card';
import { BadgeVariant, StatusBadge } from '../../components/status-badge/status-badge';
import { Avatar } from '../../components/avatar/avatar';
import { PageHeader } from '../../components/page-header/page-header';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, StatCard, StatusBadge, Avatar, PageHeader],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss',
})
export class AdminDashboard implements OnInit {
  readonly dashboard = signal<AdminDashboardData | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly today = new Date();

  constructor(private readonly adminApi: Admin) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.adminApi.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data && typeof data === 'object' && 'totalRevenue' in data ? data : null);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  totalStatus(data: AdminDashboardData): number {
    return data.upcomingBookings + data.completedBookings + data.cancelledBookings;
  }

  share(part: number, data: AdminDashboardData): number {
    const total = this.totalStatus(data);
    return total ? Math.round((part / total) * 100) : 0;
  }

  statusVariant(b: RecentBooking): BadgeVariant {
    return b.status === 'completed' ? 'success' : b.status === 'cancelled' ? 'danger' : 'info';
  }

  statusLabel(b: RecentBooking): string {
    return b.status.charAt(0).toUpperCase() + b.status.slice(1);
  }
}
