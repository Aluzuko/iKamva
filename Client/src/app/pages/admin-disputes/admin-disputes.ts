import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dispute, DisputeStatus } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { Dialog } from '../../components/dialog/dialog';
import { matchesSearch, statusView } from '../../utils/status';

type Filter = 'all' | DisputeStatus;
type Action = 'resolve' | 'reject';

@Component({
  selector: 'app-admin-disputes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, StatusBadge, Dialog],
  templateUrl: './admin-disputes.html',
  styleUrl: './admin-disputes.scss',
})
export class AdminDisputes implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly disputes = signal<Dispute[]>([]);
  readonly filter = signal<Filter>('open');
  readonly search = signal('');
  readonly target = signal<{ dispute: Dispute; action: Action } | null>(null);
  readonly saving = signal(false);

  readonly filtered = computed(() =>
    [...this.disputes()]
      .filter(
        (d) =>
          (this.filter() === 'all' || d.status === this.filter()) &&
          matchesSearch(this.search(), d.raisedByName, d.subject, d.description)
      )
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  );

  readonly filters: { value: Filter; label: string }[] = [
    { value: 'open', label: 'Open' },
    { value: 'resolved', label: 'Resolved' },
    { value: 'rejected', label: 'Rejected' },
    { value: 'all', label: 'All' },
  ];
  readonly status = statusView;
  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly adminApi: Admin,
    private readonly notifications: NotificationService
  ) {
    this.form = this.fb.group({
      resolution: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.adminApi.getDisputes().subscribe({
      next: (d) => {
        this.disputes.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  countFor(value: Filter): number {
    return value === 'all' ? this.disputes().length : this.disputes().filter((d) => d.status === value).length;
  }

  open(dispute: Dispute, action: Action): void {
    this.form.reset({ resolution: '' });
    this.target.set({ dispute, action });
  }

  get resolutionInvalid(): boolean {
    const c = this.form.get('resolution');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  submit(): void {
    const t = this.target();
    if (!t) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const resolution = (this.form.getRawValue().resolution as string).trim();
    const call =
      t.action === 'resolve'
        ? this.adminApi.resolveDispute(t.dispute.disputeId, resolution)
        : this.adminApi.rejectDispute(t.dispute.disputeId, resolution);

    this.saving.set(true);
    call.subscribe({
      next: () => {
        const status: DisputeStatus = t.action === 'resolve' ? 'resolved' : 'rejected';
        this.disputes.update((list) =>
          list.map((d) =>
            d.disputeId === t.dispute.disputeId ? { ...d, status, resolution, resolvedAt: new Date().toISOString() } : d
          )
        );
        this.saving.set(false);
        this.target.set(null);
        this.notifications.showSuccess(
          t.action === 'resolve' ? 'Dispute resolved' : 'Dispute rejected',
          `${t.dispute.raisedByName} will see your outcome.`
        );
      },
      error: () => {
        this.saving.set(false);
        this.notifications.showError('Couldn’t save', 'The dispute wasn’t changed. Try again.');
      },
    });
  }
}
