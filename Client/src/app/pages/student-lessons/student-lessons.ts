import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StudentLesson } from '../../models/student.model';
import { Student } from '../../services/student';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { StarRating } from '../../components/star-rating/star-rating';
import { Dialog } from '../../components/dialog/dialog';
import { Pager, pageOf } from '../../components/pager/pager';
import { statusView } from '../../utils/status';

type Tab = 'upcoming' | 'past';

@Component({
  selector: 'app-student-lessons',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeader, Avatar, StatusBadge, StarRating, Dialog, Pager],
  templateUrl: './student-lessons.html',
  styleUrl: './student-lessons.scss',
})
export class StudentLessons implements OnInit {
  readonly tab = signal<Tab>('upcoming');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly lessons = signal<StudentLesson[]>([]);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly expanded = signal<string | null>(null);

  readonly cancelTarget = signal<StudentLesson | null>(null);
  readonly reviewTarget = signal<StudentLesson | null>(null);
  readonly saving = signal(false);
  readonly dialogError = signal<string | null>(null);
  readonly rating = signal(0);
  readonly hoverRating = signal(0);

  readonly upcoming = computed(() =>
    this.lessons().filter((l) => l.status === 'upcoming').sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt))
  );
  readonly past = computed(() =>
    this.lessons().filter((l) => l.status !== 'upcoming').sort((a, b) => b.scheduledAt.localeCompare(a.scheduledAt))
  );
  readonly visiblePast = computed(() => pageOf(this.past(), this.page(), this.pageSize));
  readonly toReview = computed(() => this.lessons().filter((l) => l.canReview).length);

  readonly cancelForm: FormGroup;
  readonly reviewForm: FormGroup;
  readonly status = statusView;
  readonly ratingWords = ['', 'Poor', 'Fair', 'Good', 'Very good', 'Excellent'];

  constructor(
    private readonly fb: FormBuilder,
    private readonly studentApi: Student,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.cancelForm = this.fb.group({ reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]] });
    this.reviewForm = this.fb.group({ comment: ['', [Validators.maxLength(1000)]] });
  }

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.queryParamMap.get('tab') === 'past' ? 'past' : 'upcoming');
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.studentApi.getLessons().subscribe({
      next: (l) => {
        this.lessons.set(l);
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
    this.router.navigate([], { queryParams: { tab: tab === 'past' ? 'past' : null }, replaceUrl: true });
  }

  toggle(id: string): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  hoursUntil(lesson: StudentLesson): number {
    return (new Date(lesson.scheduledAt).getTime() - Date.now()) / 3_600_000;
  }

  openCancel(lesson: StudentLesson): void {
    this.cancelForm.reset({ reason: '' });
    this.dialogError.set(null);
    this.cancelTarget.set(lesson);
  }

  get reasonInvalid(): boolean {
    const c = this.cancelForm.get('reason');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  confirmCancel(): void {
    const lesson = this.cancelTarget();
    if (!lesson) {
      return;
    }
    if (this.cancelForm.invalid) {
      this.cancelForm.markAllAsTouched();
      return;
    }
    const reason = (this.cancelForm.getRawValue().reason as string).trim();
    this.saving.set(true);
    this.studentApi.cancelLesson(lesson.bookingId, reason).subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelTarget.set(null);
        this.patch(lesson.bookingId, { status: 'cancelled', cancellationReason: reason, canCancel: false });
        this.notifications.showSuccess('Lesson cancelled', `${lesson.tutorName} has been told.`);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.dialogError.set(err.error?.message ?? 'The lesson wasn’t cancelled. Try again.');
      },
    });
  }

  openReview(lesson: StudentLesson): void {
    this.reviewForm.reset({ comment: '' });
    this.rating.set(0);
    this.hoverRating.set(0);
    this.dialogError.set(null);
    this.reviewTarget.set(lesson);
  }

  submitReview(): void {
    const lesson = this.reviewTarget();
    if (!lesson) {
      return;
    }
    if (!this.rating()) {
      this.dialogError.set('Choose a star rating.');
      return;
    }
    if (this.reviewForm.invalid) {
      this.reviewForm.markAllAsTouched();
      return;
    }
    const comment = (this.reviewForm.getRawValue().comment as string).trim() || null;
    const rating = this.rating();
    this.saving.set(true);
    this.studentApi.reviewLesson(lesson.bookingId, rating, comment).subscribe({
      next: () => {
        this.saving.set(false);
        this.reviewTarget.set(null);
        this.patch(lesson.bookingId, { canReview: false, myRating: rating, myComment: comment });
        this.notifications.showSuccess('Thanks for your rating', `It helps other students choose ${lesson.tutorName.split(' ').slice(-1)[0]}.`);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.dialogError.set(err.error?.message ?? 'Your rating wasn’t saved. Try again.');
      },
    });
  }

  onStarKey(event: KeyboardEvent): void {
    const keys: Record<string, number> = { ArrowRight: 1, ArrowUp: 1, ArrowLeft: -1, ArrowDown: -1 };
    const step = keys[event.key];
    if (!step) {
      return;
    }
    event.preventDefault();
    const next = Math.min(5, Math.max(1, (this.rating() || 0) + step));
    this.rating.set(next);
    this.hoverRating.set(0);
    const group = (event.currentTarget as HTMLElement).parentElement;
    (group?.querySelectorAll('button')[next - 1] as HTMLElement | undefined)?.focus();
  }

  private patch(id: string, change: Partial<StudentLesson>): void {
    this.lessons.update((list) => list.map((l) => (l.bookingId === id ? { ...l, ...change } : l)));
  }
}
