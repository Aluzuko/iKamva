import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ChildSummary } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { ChildSummaryCard } from '../../components/child-summary-card/child-summary-card';

@Component({
  selector: 'app-parent-children',
  standalone: true,
  imports: [CommonModule, ChildSummaryCard],
  templateUrl: './parent-children.html',
  styleUrl: './parent-children.scss',
})
export class ParentChildren implements OnInit {
  readonly children = signal<ChildSummary[]>([]);
  readonly loading = signal(true);

  constructor(private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    this.parentApi.getChildren().subscribe((data) => {
      this.children.set(data);
      this.loading.set(false);
    });
  }
}
