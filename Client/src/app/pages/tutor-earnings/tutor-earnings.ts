import { CommonModule, formatCurrency } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { Earnings } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatCard } from '../../components/stat-card/stat-card';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { BarChart } from '../../components/bar-chart/bar-chart';
import { Dialog } from '../../components/dialog/dialog';
import { Pager, pageOf } from '../../components/pager/pager';

type Tab = 'lessons' | 'payouts';

@Component({
  selector: 'app-tutor-earnings',
  standalone: true,
  imports: [CommonModule, PageHeader, StatCard, StatusBadge, BarChart, Dialog, Pager],
  templateUrl: './tutor-earnings.html',
})
export class TutorEarnings implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly data = signal<Earnings | null>(null);
  readonly tab = signal<Tab>('lessons');
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly confirmOpen = signal(false);
  readonly requesting = signal(false);

  readonly visibleLessons = computed(() => pageOf(this.data()?.recentLessons ?? [], this.page(), this.pageSize));
  readonly hasEarnings = computed(() => (this.data()?.totalEarned ?? 0) > 0);

  constructor(private readonly tutorApi: Tutor, private readonly notifications: NotificationService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getEarnings().subscribe({
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

  setTab(tab: Tab): void {
    this.tab.set(tab);
    this.page.set(1);
  }

  requestPayout(): void {
    this.requesting.set(true);
    this.tutorApi.requestPayout().subscribe({
      next: (p) => {
        this.requesting.set(false);
        this.confirmOpen.set(false);
        this.notifications.showSuccess('Payout requested', `${this.money(p.amount)} is on its way. Our team processes payouts within 3 working days.`);
        this.setTab('payouts');
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.requesting.set(false);
        this.confirmOpen.set(false);
        this.notifications.showError('Payout not requested', err.error?.message ?? 'Try again.');
        this.load();
      },
    });
  }

  money(value: number): string {
    return formatCurrency(value, 'en-US', 'R', 'ZAR', '1.2-2');
  }
}
