import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FamilyDashboard } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { NotificationService } from '../../services/notification.service';
import { PayFastCheckout } from '../../services/payfast-checkout';
import { SessionService } from '../../services/session.service';
import { ChildSummaryCard } from '../../components/child-summary-card/child-summary-card';
import { LessonListItem } from '../../components/lesson-list-item/lesson-list-item';
import { StatCard } from '../../components/stat-card/stat-card';
import { PageHeader } from '../../components/page-header/page-header';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ChildSummaryCard, LessonListItem, StatCard, PageHeader],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.scss',
})
export class ParentDashboard implements OnInit {
  readonly dashboard = signal<FamilyDashboard | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly paying = signal(false);

  constructor(
    private readonly payFast: PayFastCheckout,
    private readonly parentApi: ParentApiService,
    private readonly notifications: NotificationService,
    readonly session: SessionService
  ) {}

  get greeting(): string {
    const hour = new Date().getHours();
    const part = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    return this.session.firstName ? `${part}, ${this.session.firstName}` : part;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.parentApi.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  payNow(invoiceId: string): void {
    this.paying.set(true);
    this.payFast.start(invoiceId).subscribe({
      error: (err: unknown) => {
        this.paying.set(false);
        this.notifications.push('danger', 'Payment not started', this.payFast.errorMessage(err));
      },
    });
  }
}
