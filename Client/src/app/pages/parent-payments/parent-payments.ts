import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { PaymentsOverview } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { PayFastCheckout } from '../../services/payfast-checkout';
import { NotificationService } from '../../services/notification.service';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { StatCard } from '../../components/stat-card/stat-card';
import { PageHeader } from '../../components/page-header/page-header';

@Component({
  selector: 'app-parent-payments',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge, StatCard, PageHeader],
  templateUrl: './parent-payments.html',
  styleUrl: './parent-payments.scss',
})
export class ParentPayments implements OnInit {
  readonly overview = signal<PaymentsOverview | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly payingId = signal<string | null>(null);

  readonly outstanding = computed(() =>
    (this.overview()?.history ?? []).filter((i) => i.status === 'pending').reduce((sum, i) => sum + i.amount, 0)
  );

  readonly paidTotal = computed(() =>
    (this.overview()?.history ?? []).filter((i) => i.status === 'paid').reduce((sum, i) => sum + i.amount, 0)
  );

  constructor(
    private readonly payFast: PayFastCheckout,
    private readonly parentApi: ParentApiService,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.parentApi.getPayments().subscribe({
      next: (data) => {
        this.overview.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  pay(invoiceId: string | null): void {
    if (!invoiceId || this.payingId()) {
      return;
    }
    this.payingId.set(invoiceId);
    this.payFast.start(invoiceId).subscribe({
      error: (err: unknown) => {
        this.payingId.set(null);
        this.notifications.push('danger', 'Payment not started', this.payFast.errorMessage(err));
      },
    });
  }
}
