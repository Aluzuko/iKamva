import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  BLOCKED_ATTACHMENT_EXTENSIONS,
  EMAIL_CATEGORY_LABELS,
  EmailCategory,
  EmailDetail,
  EmailListItem,
  EmailStats,
  EmailStatus,
  MAX_EMAIL_ATTACHMENTS,
  MAX_EMAIL_ATTACHMENT_BYTES,
} from '../../models/email.model';
import { AdminEmails as AdminEmailsApi } from '../../services/admin-emails';
import { FileOpener } from '../../services/file-opener';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { Dialog } from '../../components/dialog/dialog';
import { Pager } from '../../components/pager/pager';
import { statusView } from '../../utils/status';

type Filter = 'all' | EmailStatus;

@Component({
  selector: 'app-admin-emails',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, StatusBadge, Dialog, Pager],
  templateUrl: './admin-emails.html',
  styleUrl: './admin-emails.scss',
})
export class AdminEmails implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<EmailListItem[]>([]);
  readonly total = signal(0);
  readonly stats = signal<EmailStats | null>(null);
  readonly filter = signal<Filter>('all');
  readonly category = signal<EmailCategory | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly processing = signal(false);

  readonly detail = signal<EmailDetail | null>(null);
  readonly detailLoading = signal(false);
  readonly view = signal<'html' | 'text'>('html');
  readonly preview = signal<SafeHtml | null>(null);
  readonly acting = signal(false);

  readonly composeOpen = signal(false);
  readonly files = signal<File[]>([]);
  readonly fileError = signal<string | null>(null);
  readonly composeError = signal<string | null>(null);
  readonly sendingLetter = signal(false);
  readonly composeForm: FormGroup;

  readonly categories = Object.entries(EMAIL_CATEGORY_LABELS).map(([value, label]) => ({ value: value as EmailCategory, label }));
  readonly categoryLabel = EMAIL_CATEGORY_LABELS;
  readonly status = statusView;
  readonly filesSize = computed(() => this.files().reduce((t, f) => t + f.size, 0));
  readonly filters = computed<{ value: Filter; label: string; count: number | null }[]>(() => {
    const s = this.stats();
    return [
      { value: 'all', label: 'All', count: null },
      { value: 'pending', label: 'Waiting', count: s?.pending ?? null },
      { value: 'failed', label: 'Failed', count: s?.failed ?? null },
      { value: 'sent', label: 'Sent', count: s?.sent ?? null },
      { value: 'cancelled', label: 'Cancelled', count: s?.cancelled ?? null },
    ];
  });

  private readonly searchInput = new Subject<string>();
  private request: Subscription | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: AdminEmailsApi,
    private readonly files$: FileOpener,
    private readonly notifications: NotificationService,
    private readonly sanitizer: DomSanitizer,
    private readonly destroyRef: DestroyRef
  ) {
    this.composeForm = this.fb.group({
      toEmail: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
      toName: ['', [Validators.maxLength(200)]],
      subject: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(300)]],
      message: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(20000)]],
    });
  }

  ngOnInit(): void {
    this.searchInput
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => {
        this.search.set(v);
        this.page.set(1);
        this.loadList();
      });
    this.load();
  }

  load(): void {
    this.loadStats();
    this.loadList();
  }

  loadStats(): void {
    this.api.stats().subscribe({ next: (s) => this.stats.set(s) });
  }

  loadList(): void {
    this.failed.set(false);
    this.loading.set(true);
    this.request?.unsubscribe();
    const f = this.filter();
    this.request = this.api
      .list({ status: f === 'all' ? null : f, category: this.category(), search: this.search(), page: this.page(), pageSize: this.pageSize })
      .subscribe({
        next: (p) => {
          this.items.set(p.items);
          this.total.set(p.total);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.failed.set(true);
        },
      });
  }

  setFilter(value: Filter): void {
    this.filter.set(value);
    this.page.set(1);
    this.loadList();
  }

  setCategory(value: string): void {
    this.category.set((value || null) as EmailCategory | null);
    this.page.set(1);
    this.loadList();
  }

  onSearch(value: string): void {
    this.searchInput.next(value);
  }

  goTo(page: number): void {
    this.page.set(page);
    this.loadList();
  }

  process(): void {
    this.processing.set(true);
    this.api.process(50).subscribe({
      next: (r) => {
        this.processing.set(false);
        if (!r.claimed) {
          this.notifications.showSuccess('Nothing to send', 'No emails are due right now.');
        } else {
          const parts = [`${r.sent} sent`];
          if (r.retrying) {
            parts.push(`${r.retrying} will retry`);
          }
          if (r.failed) {
            parts.push(`${r.failed} failed`);
          }
          (r.failed || r.retrying ? this.notifications.showError.bind(this.notifications) : this.notifications.showSuccess.bind(this.notifications))(
            'Queue processed',
            parts.join(', ') + '.'
          );
        }
        this.load();
      },
      error: () => {
        this.processing.set(false);
        this.notifications.showError('Couldn’t process the queue', 'Check the API log for details and try again.');
      },
    });
  }

  open(item: EmailListItem): void {
    this.detail.set(null);
    this.preview.set(null);
    this.view.set('html');
    this.detailLoading.set(true);
    this.api.get(item.emailId).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.preview.set(this.sanitizer.bypassSecurityTrustHtml(d.htmlBody));
        this.detailLoading.set(false);
      },
      error: () => {
        this.detailLoading.set(false);
        this.notifications.showError('Couldn’t open the email', 'Try again.');
      },
    });
  }

  closeDetail(): void {
    this.detail.set(null);
    this.detailLoading.set(false);
  }

  download(d: EmailDetail, attachmentId: string, fileName: string): void {
    this.files$.open(this.api.attachmentUrl(d.summary.emailId, attachmentId), fileName);
  }

  retry(d: EmailDetail): void {
    this.act(this.api.retry(d.summary.emailId), 'Queued to send again', 'It goes out the next time the queue runs.');
  }

  cancel(d: EmailDetail): void {
    this.act(this.api.cancel(d.summary.emailId), 'Email cancelled', 'It won’t be sent.');
  }

  openCompose(): void {
    this.composeForm.reset({ toEmail: '', toName: '', subject: '', message: '' });
    this.files.set([]);
    this.fileError.set(null);
    this.composeError.set(null);
    this.composeOpen.set(true);
  }

  addFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const chosen = Array.from(input.files ?? []);
    input.value = '';
    this.fileError.set(null);
    const blocked = chosen.find((f) => BLOCKED_ATTACHMENT_EXTENSIONS.some((ext) => f.name.toLowerCase().endsWith(ext)));
    if (blocked) {
      this.fileError.set(`${blocked.name} can’t be attached because email providers block that file type.`);
      return;
    }
    const empty = chosen.find((f) => f.size === 0);
    if (empty) {
      this.fileError.set(`${empty.name} is empty.`);
      return;
    }
    const next = [...this.files(), ...chosen];
    if (next.length > MAX_EMAIL_ATTACHMENTS) {
      this.fileError.set(`You can attach up to ${MAX_EMAIL_ATTACHMENTS} files.`);
      return;
    }
    if (next.reduce((t, f) => t + f.size, 0) > MAX_EMAIL_ATTACHMENT_BYTES) {
      this.fileError.set('Attachments must add up to 10 MB or less.');
      return;
    }
    this.files.set(next);
  }

  removeFile(index: number): void {
    this.files.update((list) => list.filter((_, i) => i !== index));
    this.fileError.set(null);
  }

  invalid(name: string): boolean {
    const c = this.composeForm.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  sendLetter(): void {
    this.composeError.set(null);
    if (this.composeForm.invalid) {
      this.composeForm.markAllAsTouched();
      return;
    }
    const v = this.composeForm.getRawValue();
    this.sendingLetter.set(true);
    this.api
      .compose({ toEmail: v.toEmail.trim(), toName: v.toName?.trim() || null, subject: v.subject.trim(), message: v.message.trim(), files: this.files() })
      .subscribe({
        next: () => {
          this.sendingLetter.set(false);
          this.composeOpen.set(false);
          this.notifications.showSuccess('Letter queued', `It goes to ${v.toEmail.trim()} the next time the queue runs.`);
          this.load();
        },
        error: (err: HttpErrorResponse) => {
          this.sendingLetter.set(false);
          this.composeError.set(err.status === 413 ? 'Attachments are too large.' : err.error?.message ?? 'The letter wasn’t queued. Try again.');
        },
      });
  }

  size(bytes: number): string {
    return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  whenLabel(item: EmailListItem): string {
    if (item.status === 'sent' && item.sentAt) {
      return 'Sent ' + this.relative(item.sentAt);
    }
    if (item.status === 'pending') {
      const due = new Date(item.nextAttemptAt).getTime();
      return due > Date.now() ? (item.attempts ? 'Retry ' : 'Scheduled ') + this.relative(item.nextAttemptAt) : 'Due now';
    }
    return 'Queued ' + this.relative(item.createdAt);
  }

  private relative(iso: string): string {
    const diff = new Date(iso).getTime() - Date.now();
    const minutes = Math.round(Math.abs(diff) / 60000);
    const text = minutes < 1 ? 'a moment' : minutes < 60 ? `${minutes} min` : minutes < 1440 ? `${Math.round(minutes / 60)} h` : `${Math.round(minutes / 1440)} d`;
    return diff > 0 ? `in ${text}` : minutes < 1 ? 'just now' : `${text} ago`;
  }

  private act(call: ReturnType<AdminEmailsApi['retry']>, title: string, message: string): void {
    this.acting.set(true);
    call.subscribe({
      next: () => {
        this.acting.set(false);
        this.closeDetail();
        this.notifications.showSuccess(title, message);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.acting.set(false);
        this.notifications.showError('Couldn’t update the email', err.error?.message ?? 'Try again.');
      },
    });
  }
}
