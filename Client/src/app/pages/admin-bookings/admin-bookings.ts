import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminBooking, AdminInvoice } from '../../models/admin.model';
import { BookingStatus, InvoiceStatus } from '../../models/parent.model';
import { Admin } from '../../services/admin';
import { AdminEmails } from '../../services/admin-emails';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { StatCard } from '../../components/stat-card/stat-card';
import { Pager, pageOf } from '../../components/pager/pager';
import { matchesSearch, statusView } from '../../utils/status';

type Tab = 'bookings' | 'payments';

@Component({
  selector: 'app-admin-bookings',
  standalone: true,
  imports: [CommonModule, PageHeader, Avatar, StatusBadge, StatCard, Pager],
  templateUrl: './admin-bookings.html',
})
export class AdminBookings implements OnInit {
  readonly tab = signal<Tab>('bookings');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly bookings = signal<AdminBooking[]>([]);
  readonly invoices = signal<AdminInvoice[]>([]);

  readonly bookingFilter = signal<'all' | BookingStatus>('all');
  readonly invoiceFilter = signal<'all' | InvoiceStatus>('all');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly filteredBookings = computed(() =>
    [...this.bookings()]
      .filter(
        (b) =>
          (this.bookingFilter() === 'all' || b.status === this.bookingFilter()) &&
          matchesSearch(this.search(), b.studentName, b.tutorName, b.subjectName)
      )
      .sort((a, b) => b.scheduledAt.localeCompare(a.scheduledAt))
  );
  readonly filteredInvoices = computed(() =>
    this.invoices().filter(
      (i) =>
        (this.invoiceFilter() === 'all' || i.status === this.invoiceFilter()) &&
        matchesSearch(this.search(), i.parentName, i.studentName, i.periodDescription)
    )
  );
  readonly visibleBookings = computed(() => pageOf(this.filteredBookings(), this.page(), this.pageSize));
  readonly visibleInvoices = computed(() => pageOf(this.filteredInvoices(), this.page(), this.pageSize));

  readonly collected = computed(() => this.sum(this.invoices().filter((i) => i.status === 'paid')));
  readonly outstanding = computed(() => this.sum(this.invoices().filter((i) => i.status === 'pending')));
  readonly bookedValue = computed(() =>
    this.bookings().filter((b) => b.status !== 'cancelled').reduce((t, b) => t + b.price, 0)
  );

  readonly bookingFilters: { value: 'all' | BookingStatus; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'upcoming', label: 'Upcoming' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' },
  ];
  readonly invoiceFilters: { value: 'all' | InvoiceStatus; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'pending', label: 'Outstanding' },
    { value: 'paid', label: 'Paid' },
  ];
  readonly status = statusView;

  readonly emailingId = signal<string | null>(null);

  constructor(
    private readonly adminApi: Admin,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly emails: AdminEmails,
    private readonly notifications: NotificationService
  ) {}

  emailInvoice(invoice: AdminInvoice): void {
    this.emailingId.set(invoice.invoiceId);
    this.emails.emailInvoice(invoice.invoiceId).subscribe({
      next: () => {
        this.emailingId.set(null);
        this.notifications.showSuccess('Invoice queued', `The PDF goes to ${invoice.parentName} the next time the email queue runs.`);
      },
      error: () => {
        this.emailingId.set(null);
        this.notifications.showError('Invoice not queued', 'Try again.');
      },
    });
  }

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.queryParamMap.get('tab') === 'payments' ? 'payments' : 'bookings');
    this.load();
  }

  load(): void {
    this.failed.set(false);
    forkJoin({ bookings: this.adminApi.getBookings(), invoices: this.adminApi.getPayments() }).subscribe({
      next: ({ bookings, invoices }) => {
        this.bookings.set(bookings);
        this.invoices.set(invoices);
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
    this.router.navigate([], { queryParams: { tab: tab === 'payments' ? 'payments' : null }, replaceUrl: true });
  }

  setBookingFilter(value: 'all' | BookingStatus): void {
    this.bookingFilter.set(value);
    this.page.set(1);
  }

  setInvoiceFilter(value: 'all' | InvoiceStatus): void {
    this.invoiceFilter.set(value);
    this.page.set(1);
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  bookingCount(value: 'all' | BookingStatus): number {
    return value === 'all' ? this.bookings().length : this.bookings().filter((b) => b.status === value).length;
  }

  invoiceCount(value: 'all' | InvoiceStatus): number {
    return value === 'all' ? this.invoices().length : this.invoices().filter((i) => i.status === value).length;
  }

  private sum(list: AdminInvoice[]): number {
    return list.reduce((t, i) => t + i.amount, 0);
  }
}
