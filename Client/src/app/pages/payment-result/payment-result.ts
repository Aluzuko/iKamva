import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { timeout as rxTimeout } from 'rxjs';
import { PaymentAttempt } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { PayFastCheckout } from '../../services/payfast-checkout';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { UserRole } from '../../models/user-role.model';

type View = 'checking' | 'paid' | 'waiting' | 'cancelled' | 'failed' | 'missing';

const POLL_MS = 2_000;
const MAX_WAIT_MS = 30_000;
const REQUEST_TIMEOUT_MS = 10_000;

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
  readonly accountBase = signal<'student' | 'parent'>('parent');

  private attemptId: string | null = null;
  private polls = 0;
  private pollingDeadline = 0;
  private checkInProgress = false;
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parentApi: ParentApiService,
    private readonly payFast: PayFastCheckout,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    const role = this.route.snapshot.pathFromRoot
      .map((route) => route.data['role'])
      .find((value): value is UserRole => value === UserRole.Student || value === UserRole.Parent);
    if (role === UserRole.Student) {
      this.accountBase.set('student');
    }

    const q = this.route.snapshot.queryParamMap;
    this.attemptId = q.get('attempt');
    if (!this.attemptId) {
      this.view.set('missing');
      return;
    }
    this.pollingDeadline = Date.now() + MAX_WAIT_MS;
    this.check(q.get('cancelled') === '1');
  }

  ngOnDestroy(): void {
    if (this.timer) {
      clearTimeout(this.timer);
    }
  }

  check(cancelled = false): void {
    if (!this.attemptId || this.checkInProgress) {
      return;
    }
    if (Date.now() >= this.pollingDeadline) {
      this.view.set('waiting');
      return;
    }

    this.checkInProgress = true;
    this.parentApi.getPaymentAttempt(this.attemptId, this.polls > 0)
      .pipe(rxTimeout({ first: REQUEST_TIMEOUT_MS }))
      .subscribe({
        next: (a) => {
          this.checkInProgress = false;
          this.attempt.set(a);
          if (a.status === 'complete' || a.invoiceStatus === 'paid') {
            this.view.set('paid');
          } else if (a.status === 'failed') {
            this.view.set('failed');
          } else if (cancelled || a.status === 'cancelled') {
            this.view.set('cancelled');
          } else {
            this.polls++;
            this.scheduleNextCheck();
          }
        },
        error: (err: HttpErrorResponse) => {
          this.checkInProgress = false;
          if (err.status === 404) {
            this.view.set('missing');
          } else {
            this.polls++;
            this.scheduleNextCheck(cancelled);
          }
        },
      });
  }

  checkAgain(): void {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    this.polls = 0;
    this.pollingDeadline = Date.now() + MAX_WAIT_MS;
    this.view.set('checking');
    this.check();
  }

  private scheduleNextCheck(cancelled = false): void {
    const remaining = this.pollingDeadline - Date.now();
    if (remaining <= 0) {
      this.view.set('waiting');
      return;
    }

    this.view.set('checking');
    this.timer = setTimeout(() => this.check(cancelled), Math.min(POLL_MS, remaining));
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
