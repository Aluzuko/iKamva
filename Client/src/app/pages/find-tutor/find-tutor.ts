import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { SubjectRef, TutorCard, TutorSearch } from '../../models/student.model';
import { Student } from '../../services/student';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StarRating } from '../../components/star-rating/star-rating';

@Component({
  selector: 'app-find-tutor',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader, Avatar, StarRating],
  templateUrl: './find-tutor.html',
  styleUrl: './find-tutor.scss',
})
export class FindTutor implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly tutors = signal<TutorCard[]>([]);
  readonly subjects = signal<SubjectRef[]>([]);
  readonly filters = signal<TutorSearch>({ sort: 'recommended' });

  readonly priceOptions = [
    { value: null, label: 'Any price' },
    { value: 200, label: 'Up to R200/hr' },
    { value: 300, label: 'Up to R300/hr' },
    { value: 400, label: 'Up to R400/hr' },
    { value: 500, label: 'Up to R500/hr' },
  ];
  readonly ratingOptions = [
    { value: null, label: 'Any rating' },
    { value: 4, label: '4+ stars' },
    { value: 4.5, label: '4.5+ stars' },
  ];
  readonly sortOptions = [
    { value: 'recommended', label: 'Recommended' },
    { value: 'rating', label: 'Highest rated' },
    { value: 'priceLow', label: 'Price: low to high' },
    { value: 'priceHigh', label: 'Price: high to low' },
    { value: 'experience', label: 'Most experienced' },
  ];

  private readonly searchInput = new Subject<string>();
  private request: Subscription | null = null;

  constructor(
    private readonly studentApi: Student,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.filters.set({
      search: q.get('q'),
      subjectId: q.get('subject'),
      maxRate: q.get('max') ? Number(q.get('max')) : null,
      minRating: q.get('rating') ? Number(q.get('rating')) : null,
      sort: q.get('sort') ?? 'recommended',
    });

    this.studentApi.getSubjects().subscribe({ next: (s) => this.subjects.set(s) });

    this.searchInput
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.update({ search: value.trim() || null }));

    this.load();
  }

  onSearch(value: string): void {
    this.searchInput.next(value);
  }

  update(change: Partial<TutorSearch>): void {
    this.filters.update((f) => ({ ...f, ...change }));
    const f = this.filters();
    this.router.navigate([], {
      queryParams: {
        q: f.search || null,
        subject: f.subjectId || null,
        max: f.maxRate || null,
        rating: f.minRating || null,
        sort: f.sort && f.sort !== 'recommended' ? f.sort : null,
      },
      replaceUrl: true,
    });
    this.load();
  }

  clear(): void {
    this.filters.set({ sort: 'recommended' });
    this.router.navigate([], { queryParams: {}, replaceUrl: true });
    this.load();
  }

  get hasFilters(): boolean {
    const f = this.filters();
    return !!(f.search || f.subjectId || f.maxRate || f.minRating);
  }

  load(): void {
    this.failed.set(false);
    this.loading.set(true);
    this.request?.unsubscribe();
    this.request = this.studentApi.searchTutors(this.filters()).subscribe({
      next: (t) => {
        this.tutors.set(t);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  num(value: string): number | null {
    return value === '' ? null : Number(value);
  }
}
