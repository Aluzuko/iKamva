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
import { Avatar } from '../../components/avatar/avatar';
import { Pager, pageOf } from '../../components/pager/pager';
import { matchesSearch } from '../../utils/status';

type Filter = 'missing' | 'written' | 'all';

const URL_PATTERN = /^https?:\/\/\S+$/i;

@Component({
  selector: 'app-tutor-notes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, Dialog, Avatar, Pager],
  templateUrl: './tutor-notes.html',
  styleUrl: './tutor-notes.scss',
})
export class TutorNotes implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly lessons = signal<TutorLesson[]>([]);
  readonly filter = signal<Filter>('missing');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly editing = signal<TutorLesson | null>(null);
  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly completed = computed(() =>
    this.lessons().filter((l) => l.status === 'completed').sort((a, b) => b.scheduledAt.localeCompare(a.scheduledAt))
  );
  readonly filtered = computed(() =>
    this.completed().filter(
      (l) =>
        (this.filter() === 'all' || (this.filter() === 'missing') === !l.notes) &&
        matchesSearch(this.search(), l.studentName, l.subjectName, l.notes)
    )
  );
  readonly visible = computed(() => pageOf(this.filtered(), this.page(), this.pageSize));

  readonly filters: { value: Filter; label: string }[] = [
    { value: 'missing', label: 'Needs notes' },
    { value: 'written', label: 'Written' },
    { value: 'all', label: 'All' },
  ];

  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly tutorApi: Tutor,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.form = this.fb.group({
      notes: ['', [Validators.required, Validators.maxLength(4000)]],
      recordingUrl: ['', [Validators.maxLength(500), Validators.pattern(URL_PATTERN)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getLessons().subscribe({
      next: (l) => {
        this.lessons.set(l);
        this.loading.set(false);
        if (!this.completed().some((x) => !x.notes)) {
          this.filter.set('all');
        }
        const requested = this.route.snapshot.queryParamMap.get('lesson');
        const lesson = requested ? this.completed().find((x) => x.bookingId === requested) : undefined;
        if (lesson) {
          this.open(lesson);
        }
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  setFilter(value: Filter): void {
    this.filter.set(value);
    this.page.set(1);
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  countFor(value: Filter): number {
    const all = this.completed();
    return value === 'all' ? all.length : all.filter((l) => (value === 'missing') === !l.notes).length;
  }

  open(lesson: TutorLesson): void {
    this.saveError.set(null);
    this.form.reset({ notes: lesson.notes ?? '', recordingUrl: lesson.recordingUrl ?? '' });
    this.editing.set(lesson);
  }

  close(): void {
    this.editing.set(null);
    if (this.route.snapshot.queryParamMap.has('lesson')) {
      this.router.navigate([], { queryParams: { lesson: null }, replaceUrl: true });
    }
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  save(): void {
    const lesson = this.editing();
    if (!lesson) {
      return;
    }
    const notes = (this.form.get('notes')?.value as string).trim();
    if (!notes) {
      this.form.get('notes')?.setValue('');
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const url = (this.form.get('recordingUrl')?.value as string).trim() || null;
    this.saving.set(true);
    this.tutorApi.updateNotes(lesson.bookingId, notes, url).subscribe({
      next: () => {
        this.saving.set(false);
        this.lessons.update((list) => list.map((l) => (l.bookingId === lesson.bookingId ? { ...l, notes, recordingUrl: url } : l)));
        this.close();
        this.notifications.showSuccess('Notes saved', `${lesson.studentName.split(' ')[0]}’s parent can now read them.`);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.saveError.set(err.error?.message ?? 'Your notes weren’t saved. Try again.');
      },
    });
  }
}
