import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FamilyDashboard } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { ChildSummaryCard } from '../../components/child-summary-card/child-summary-card';
import { LessonListItem } from '../../components/lesson-list-item/lesson-list-item';
import { StatCard } from '../../components/stat-card/stat-card';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ChildSummaryCard, LessonListItem, StatCard],
  templateUrl: './parent-dashboard.html',
  styleUrl: './parent-dashboard.scss',
})
export class ParentDashboard implements OnInit {
  readonly dashboard = signal<FamilyDashboard | null>(null);
  readonly loading = signal(true);

  constructor(private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    this.parentApi.getDashboard().subscribe((data) => {
      this.dashboard.set(data);
      this.loading.set(false);
    });
  }

  payNow(invoiceId: string): void {
    this.parentApi.payInvoice(invoiceId).subscribe(() => this.ngOnInit());
  }
}
