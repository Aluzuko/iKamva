import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PaymentAttempt } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { PayFastCheckout } from '../../services/payfast-checkout';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';

type View = 'checking' | 'paid' | 'waiting' | 'cancelled' | 'failed' | 'missing';

const POLL_MS = 2_000;
const MAX_POLLS = 30;

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader],
  templateUrl: './payment-result.html',
  styleUrl: './payment-result.scss',
})
export class PaymentResult implements OnInit, OnDestroy {
  readonly view = signal<View>('checking');
  readonly attempt = signal<PaymentAttempt | null>(null);
  readonly retrying = signal(false);

  private attemptId: string | null = null;
  private polls = 0;
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parentApi: ParentApiService,
    private readonly payFast: PayFastCheckout,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.attemptId = q.get('attempt');
    if (!this.attemptId) {
      this.view.set('missing');
      return;
    }
    this.check(q.get('cancelled') === '1');
  }

  ngOnDestroy(): void {
    if (this.timer) {
      clearTimeout(this.timer);
    }
  }

  check(cancelled = false): void {
    if (!this.attemptId) {
      return;
    }
    this.parentApi.getPaymentAttempt(this.attemptId, this.polls > 0).subscribe({
      next: (a) => {
        this.attempt.set(a);
        if (a.status === 'complete' || a.invoiceStatus === 'paid') {
          this.view.set('paid');
        } else if (a.status === 'failed') {
          this.view.set('failed');
        } else if (cancelled || a.status === 'cancelled') {
          this.view.set('cancelled');
        } else if (++this.polls >= MAX_POLLS) {
          this.view.set('waiting');
        } else {
          this.view.set('checking');
          this.timer = setTimeout(() => this.check(), POLL_MS);
        }
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.view.set('missing');
        } else if (++this.polls < MAX_POLLS) {
          this.timer = setTimeout(() => this.check(cancelled), POLL_MS);
        } else {
          this.view.set('waiting');
        }
      },
    });
  }

  checkAgain(): void {
    this.polls = 0;
    this.view.set('checking');
    this.check();
  }

  retry(): void {
    const a = this.attempt();
    if (!a) {
      return;
    }
    this.retrying.set(true);
    this.payFast.start(a.invoiceId).subscribe({
      error: (err: unknown) => {
        this.retrying.set(false);
        this.notifications.showError('Payment not started', this.payFast.errorMessage(err));
      },
    });
  }
}
