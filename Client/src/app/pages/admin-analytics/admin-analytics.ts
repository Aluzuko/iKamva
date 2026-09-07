import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Analytics } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { BarChart } from '../../components/bar-chart/bar-chart';

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, BarChart],
  templateUrl: './admin-analytics.html',
  styleUrl: './admin-analytics.scss',
})
export class AdminAnalytics implements OnInit {
  readonly analytics = signal<Analytics | null>(null);
  readonly loading = signal(true);

  constructor(private readonly adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.adminApi.getAnalytics().subscribe((data) => {
      this.analytics.set(data);
      this.loading.set(false);
    });
  }
}
