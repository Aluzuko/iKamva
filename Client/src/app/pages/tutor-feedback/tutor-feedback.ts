import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { Feedback } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { Pager, pageOf } from '../../components/pager/pager';

@Component({
  selector: 'app-tutor-feedback',
  standalone: true,
  imports: [CommonModule, PageHeader, Avatar, Pager],
  templateUrl: './tutor-feedback.html',
  styleUrl: './tutor-feedback.scss',
})
export class TutorFeedback implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly data = signal<Feedback | null>(null);
  readonly stars = signal<number | null>(null);
  readonly withComments = signal(false);
  readonly page = signal(1);
  readonly pageSize = 10;

  readonly filtered = computed(() =>
    (this.data()?.reviews ?? []).filter(
      (r) => (this.stars() === null || r.rating === this.stars()) && (!this.withComments() || !!r.comment)
    )
  );
  readonly visible = computed(() => pageOf(this.filtered(), this.page(), this.pageSize));
  readonly maxCount = computed(() => Math.max(1, ...(this.data()?.distribution ?? [0])));

  readonly scale = [5, 4, 3, 2, 1];

  constructor(private readonly tutorApi: Tutor) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getFeedback().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  setStars(value: number | null): void {
    this.stars.set(this.stars() === value ? null : value);
    this.page.set(1);
  }

  toggleComments(): void {
    this.withComments.update((v) => !v);
    this.page.set(1);
  }

  countFor(star: number): number {
    return this.data()?.distribution[star - 1] ?? 0;
  }

  starIcons(rating: number): string[] {
    return [1, 2, 3, 4, 5].map((i) => (rating >= i ? 'bi-star-fill' : rating >= i - 0.5 ? 'bi-star-half' : 'bi-star'));
  }
}
