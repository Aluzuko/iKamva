import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Dispute } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { BadgeVariant, StatusBadge } from '../../components/status-badge/status-badge';

type Tab = 'open' | 'resolved';

@Component({
  selector: 'app-admin-disputes',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge],
  templateUrl: './admin-disputes.html',
  styleUrl: './admin-disputes.scss',
})
export class AdminDisputes implements OnInit {
  readonly disputes = signal<Dispute[]>([]);
  readonly loading = signal(true);
  readonly activeTab = signal<Tab>('open');
  readonly respondingId = signal<string | null>(null);
  readonly resolutionText = signal('');

  constructor(private readonly adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.adminApi.getDisputes().subscribe((disputes) => {
      this.disputes.set(disputes);
      this.loading.set(false);
    });
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  get filtered(): Dispute[] {
    return this.disputes().filter((d) => (this.activeTab() === 'open' ? d.status === 'open' : d.status !== 'open'));
  }

  startRespond(disputeId: string): void {
    this.respondingId.set(disputeId);
    this.resolutionText.set('');
  }

  cancelRespond(): void {
    this.respondingId.set(null);
  }

  resolve(disputeId: string): void {
    if (!this.resolutionText().trim()) {
      return;
    }
    this.adminApi.resolveDispute(disputeId, this.resolutionText().trim()).subscribe(() => {
      this.respondingId.set(null);
      this.load();
    });
  }

  reject(disputeId: string): void {
    if (!this.resolutionText().trim()) {
      return;
    }
    this.adminApi.rejectDispute(disputeId, this.resolutionText().trim()).subscribe(() => {
      this.respondingId.set(null);
      this.load();
    });
  }

  statusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'resolved':
        return 'success';
      case 'rejected':
        return 'danger';
      default:
        return 'warning';
    }
  }

  statusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }
}
