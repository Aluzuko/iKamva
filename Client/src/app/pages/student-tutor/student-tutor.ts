import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LESSON_DURATIONS, SAST, Slots, TutorPublicProfile } from '../../models/student.model';
import { DAY_NAMES, minutesToTime } from '../../models/tutor.model';
import { Student } from '../../services/student';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StarRating } from '../../components/star-rating/star-rating';
import { Dialog } from '../../components/dialog/dialog';

@Component({
  selector: 'app-student-tutor',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader, Avatar, StarRating, Dialog],
  templateUrl: './student-tutor.html',
  styleUrl: './student-tutor.scss',
})
export class StudentTutor implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly notFound = signal(false);
  readonly tutor = signal<TutorPublicProfile | null>(null);

  readonly subjectId = signal<string | null>(null);
  readonly duration = signal(60);
  readonly slots = signal<Slots | null>(null);
  readonly slotsLoading = signal(false);
  readonly slotsFailed = signal(false);
  readonly selectedDate = signal<string | null>(null);
  readonly selectedStart = signal<string | null>(null);
  readonly confirmOpen = signal(false);
  readonly booking = signal(false);
  readonly bookError = signal<string | null>(null);

  readonly durations = LESSON_DURATIONS;
  readonly tz = SAST;

  readonly selectedDay = computed(() => this.slots()?.days.find((d) => d.date === this.selectedDate()) ?? null);
  readonly price = computed(() => {
    const t = this.tutor();
    return t ? Math.round(t.hourlyRate * this.duration() * 100 / 60) / 100 : 0;
  });
  readonly subjectName = computed(() => this.tutor()?.subjects.find((s) => s.subjectId === this.subjectId())?.name ?? '');
  readonly canBook = computed(() => !!this.subjectId() && !!this.selectedStart());
  readonly weekly = computed(() => {
    const t = this.tutor();
    if (!t) {
      return [];
    }
    return [1, 2, 3, 4, 5, 6, 0]
      .map((d) => ({
        day: DAY_NAMES[d].slice(0, 3),
        hours: t.availability.filter((a) => a.dayOfWeek === d).map((a) => `${minutesToTime(a.startMinute)}–${minutesToTime(a.endMinute)}`),
      }))
      .filter((d) => d.hours.length);
  });

  private tutorId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly studentApi: Student,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    this.tutorId = this.route.snapshot.paramMap.get('tutorId') ?? '';
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.notFound.set(false);
    this.studentApi.getTutor(this.tutorId).subscribe({
      next: (t) => {
        this.tutor.set(t);
        const preferred = this.route.snapshot.queryParamMap.get('subject');
        this.subjectId.set(t.subjects.find((s) => s.subjectId === preferred)?.subjectId ?? (t.subjects.length === 1 ? t.subjects[0].subjectId : null));
        this.loading.set(false);
        this.loadSlots();
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.failed.set(true);
        }
      },
    });
  }

  loadSlots(keepDate = false): void {
    this.slotsLoading.set(true);
    this.slotsFailed.set(false);
    this.selectedStart.set(null);
    this.studentApi.getSlots(this.tutorId, this.duration()).subscribe({
      next: (s) => {
        this.slots.set(s);
        this.slotsLoading.set(false);
        const current = keepDate ? s.days.find((d) => d.date === this.selectedDate() && d.starts.length) : undefined;
        this.selectedDate.set(current?.date ?? s.days.find((d) => d.starts.length)?.date ?? null);
      },
      error: () => {
        this.slotsLoading.set(false);
        this.slotsFailed.set(true);
      },
    });
  }

  setDuration(minutes: number): void {
    if (minutes !== this.duration()) {
      this.duration.set(minutes);
      this.loadSlots(true);
    }
  }

  selectDate(date: string): void {
    this.selectedDate.set(date);
    this.selectedStart.set(null);
  }

  dayDate(date: string): Date {
    const [y, m, d] = date.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  priceFor(minutes: number): number {
    const t = this.tutor();
    return t ? Math.round(t.hourlyRate * minutes * 100 / 60) / 100 : 0;
  }

  openConfirm(): void {
    this.bookError.set(null);
    if (this.canBook()) {
      this.confirmOpen.set(true);
    }
  }

  confirm(): void {
    const start = this.selectedStart();
    const subjectId = this.subjectId();
    if (!start || !subjectId) {
      return;
    }
    this.booking.set(true);
    this.studentApi.book({ tutorId: this.tutorId, subjectId, startUtc: start, durationMinutes: this.duration() }).subscribe({
      next: () => {
        this.booking.set(false);
        this.confirmOpen.set(false);
        this.notifications.showSuccess('Lesson booked', `${this.subjectName()} with ${this.tutor()?.fullName} is in your lessons.`);
        this.router.navigate(['/student/lessons']);
      },
      error: (err: HttpErrorResponse) => {
        this.booking.set(false);
        this.confirmOpen.set(false);
        this.bookError.set(err.error?.message ?? 'The lesson wasn’t booked. Try again.');
        if (err.status === 409) {
          this.loadSlots(true);
        }
      },
    });
  }
}
