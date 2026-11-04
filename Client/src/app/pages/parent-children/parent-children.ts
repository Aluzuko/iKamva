import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChildSummary } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { ChildSummaryCard } from '../../components/child-summary-card/child-summary-card';
import { PageHeader } from '../../components/page-header/page-header';

@Component({
  selector: 'app-parent-children',
  standalone: true,
  imports: [CommonModule, RouterLink, ChildSummaryCard, PageHeader],
  templateUrl: './parent-children.html',
  styleUrl: './parent-children.scss',
})
export class ParentChildren implements OnInit {
  readonly children = signal<ChildSummary[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);

  constructor(private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.parentApi.getChildren().subscribe({
      next: (data) => {
        this.children.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }
}
