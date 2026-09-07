import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AdminBooking, AdminInvoice } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { BadgeVariant, StatusBadge } from '../../components/status-badge/status-badge';

type Tab = 'bookings' | 'payments';

@Component({
  selector: 'app-admin-bookings',
  standalone: true,
  imports: [CommonModule, StatusBadge],
  templateUrl: './admin-bookings.html',
  styleUrl: './admin-bookings.scss',
})
export class AdminBookings implements OnInit {
  readonly activeTab = signal<Tab>('bookings');
  readonly loading = signal(true);
  readonly bookings = signal<AdminBooking[]>([]);
  readonly payments = signal<AdminInvoice[]>([]);

  constructor(private readonly adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.adminApi.getBookings().subscribe((bookings) => {
      this.bookings.set(bookings);
      this.loading.set(false);
    });
    this.adminApi.getPayments().subscribe((payments) => this.payments.set(payments));
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  bookingStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'completed':
        return 'success';
      case 'cancelled':
        return 'danger';
      default:
        return 'info';
    }
  }

  paymentStatusVariant(status: string): BadgeVariant {
    return status === 'paid' ? 'success' : 'warning';
  }

  label(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }
}
