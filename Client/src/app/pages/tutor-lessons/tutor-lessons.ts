import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TutorLesson } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Dialog } from '../../components/dialog/dialog';
import { Pager, pageOf } from '../../components/pager/pager';
import { TutorLessonRow } from '../../components/tutor-lesson-row/tutor-lesson-row';
import { matchesSearch } from '../../utils/status';

type Tab = 'upcoming' | 'past';
type PastFilter = 'all' | 'completed' | 'cancelled';

@Component({
  selector: 'app-tutor-lessons',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, Dialog, Pager, TutorLessonRow],
  templateUrl: './tutor-lessons.html',
})
export class TutorLessons implements OnInit {
  readonly tab = signal<Tab>('upcoming');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly lessons = signal<TutorLesson[]>([]);
  readonly search = signal('');
  readonly pastFilter = signal<PastFilter>('all');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly busyId = signal<string | null>(null);
  readonly cancelTarget = signal<TutorLesson | null>(null);
  readonly cancelError = signal<string | null>(null);

  readonly toConfirm = computed(() =>
    this.lessons().filter((l) => l.needsCompletion).sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt))
  );
  readonly upcoming = computed(() =>
    this.lessons()
      .filter((l) => l.status === 'upcoming' && !l.needsCompletion && matchesSearch(this.search(), l.studentName, l.subjectName))
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt))
  );
  readonly past = computed(() =>
    this.lessons()
      .filter(
        (l) =>
          l.status !== 'upcoming' &&
          (this.pastFilter() === 'all' || l.status === this.pastFilter()) &&
          matchesSearch(this.search(), l.studentName, l.subjectName)
      )
      .sort((a, b) => b.scheduledAt.localeCompare(a.scheduledAt))
  );
  readonly visiblePast = computed(() => pageOf(this.past(), this.page(), this.pageSize));

  readonly pastFilters: { value: PastFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' },
  ];

  readonly cancelForm: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly tutorApi: Tutor,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.cancelForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
    });
  }

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.queryParamMap.get('tab') === 'past' ? 'past' : 'upcoming');
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getLessons().subscribe({
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

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  setPastFilter(value: PastFilter): void {
    this.pastFilter.set(value);
    this.page.set(1);
  }

  pastCount(value: PastFilter): number {
    const past = this.lessons().filter((l) => l.status !== 'upcoming');
    return value === 'all' ? past.length : past.filter((l) => l.status === value).length;
  }

  complete(lesson: TutorLesson): void {
    this.busyId.set(lesson.bookingId);
    this.tutorApi.completeLesson(lesson.bookingId).subscribe({
      next: () => {
        this.busyId.set(null);
        this.patch(lesson.bookingId, { status: 'completed', needsCompletion: false });
        this.notifications.showSuccess('Lesson completed', `Add notes so ${lesson.studentName.split(' ')[0]}’s parent can see how it went.`);
      },
      error: (err: HttpErrorResponse) => {
        this.busyId.set(null);
        this.notifications.showError('Couldn’t update the lesson', err.error?.message ?? 'Try again.');
      },
    });
  }

  openCancel(lesson: TutorLesson): void {
    this.cancelForm.reset({ reason: '' });
    this.cancelError.set(null);
    this.cancelTarget.set(lesson);
  }

  get reasonInvalid(): boolean {
    const c = this.cancelForm.get('reason');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  hoursUntil(lesson: TutorLesson): number {
    return (new Date(lesson.scheduledAt).getTime() - Date.now()) / 3_600_000;
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
    this.busyId.set(lesson.bookingId);
    this.tutorApi.cancelLesson(lesson.bookingId, reason).subscribe({
      next: () => {
        this.busyId.set(null);
        this.cancelTarget.set(null);
        this.patch(lesson.bookingId, { status: 'cancelled', cancellationReason: reason });
        this.notifications.showSuccess('Lesson cancelled', `${lesson.studentName} will see your reason.`);
      },
      error: (err: HttpErrorResponse) => {
        this.busyId.set(null);
        this.cancelError.set(err.error?.message ?? 'The lesson wasn’t cancelled. Try again.');
      },
    });
  }

  private patch(id: string, change: Partial<TutorLesson>): void {
    this.lessons.update((list) => list.map((l) => (l.bookingId === id ? { ...l, ...change } : l)));
  }
}
