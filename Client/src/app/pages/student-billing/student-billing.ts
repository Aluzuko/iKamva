import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StudentBilling as Billing } from '../../models/student.model';
import { Student } from '../../services/student';
import { PageHeader } from '../../components/page-header/page-header';
import { StatCard } from '../../components/stat-card/stat-card';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { statusView } from '../../utils/status';

@Component({
  selector: 'app-student-billing',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader, StatCard, StatusBadge],
  templateUrl: './student-billing.html',
})
export class StudentBilling implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly billing = signal<Billing | null>(null);
  readonly status = statusView;
  readonly today = new Date();

  constructor(private readonly studentApi: Student) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.studentApi.getBilling().subscribe({
      next: (b) => {
        this.billing.set(b);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  payerNames(b: Billing): string {
    return b.parents.map((p) => p.fullName).join(' and ');
  }
}
