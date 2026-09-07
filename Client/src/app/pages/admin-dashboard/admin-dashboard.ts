import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AdminDashboard as AdminDashboardModel } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { StatCard } from '../../components/stat-card/stat-card';
import { StatusBadge, BadgeVariant } from '../../components/status-badge/status-badge';
import { Avatar } from '../../components/avatar/avatar';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, StatCard, StatusBadge, Avatar],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss',
})
export class AdminDashboard implements OnInit {
  readonly dashboard = signal<AdminDashboardModel | null>(null);
  readonly loading = signal(true);

  constructor(private readonly adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.adminApi.getDashboard().subscribe((data) => {
      this.dashboard.set(data);
      this.loading.set(false);
    });
  }

  statusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'completed':
        return 'success';
      case 'cancelled':
        return 'danger';
      default:
        return 'info';
    }
  }

  statusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }
}
