import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { PaymentsOverview } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';

@Component({
  selector: 'app-parent-payments',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge],
  templateUrl: './parent-payments.html',
  styleUrl: './parent-payments.scss',
})
export class ParentPayments implements OnInit {
  readonly overview = signal<PaymentsOverview | null>(null);
  readonly loading = signal(true);

  constructor(private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    this.parentApi.getPayments().subscribe((data) => {
      this.overview.set(data);
      this.loading.set(false);
    });
  }

  pay(invoiceId: string | null): void {
    if (!invoiceId) {
      return;
    }
    this.parentApi.payInvoice(invoiceId).subscribe(() => this.ngOnInit());
  }
}
