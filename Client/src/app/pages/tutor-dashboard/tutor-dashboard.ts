import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TutorDashboard as Dashboard, TutorLesson } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { SessionService } from '../../services/session.service';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatCard } from '../../components/stat-card/stat-card';
import { TutorLessonRow } from '../../components/tutor-lesson-row/tutor-lesson-row';

@Component({
  selector: 'app-tutor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader, StatCard, TutorLessonRow],
  templateUrl: './tutor-dashboard.html',
})
export class TutorDashboard implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly dashboard = signal<Dashboard | null>(null);
  readonly busyId = signal<string | null>(null);

  constructor(
    private readonly tutorApi: Tutor,
    private readonly session: SessionService,
    private readonly notifications: NotificationService
  ) {}

  get greeting(): string {
    const hour = new Date().getHours();
    const part = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    return this.session.firstName ? `${part}, ${this.session.firstName}` : part;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getDashboard().subscribe({
      next: (d) => {
        this.dashboard.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  complete(lesson: TutorLesson): void {
    this.busyId.set(lesson.bookingId);
    this.tutorApi.completeLesson(lesson.bookingId).subscribe({
      next: () => {
        this.busyId.set(null);
        this.notifications.showSuccess('Lesson completed', 'Remember to add notes for it.');
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.busyId.set(null);
        this.notifications.showError('Couldn’t update the lesson', err.error?.message ?? 'Try again.');
      },
    });
  }

  minutesUntil(lesson: TutorLesson): number {
    return Math.round((new Date(lesson.scheduledAt).getTime() - Date.now()) / 60_000);
  }

  startsIn(lesson: TutorLesson): string {
    const minutes = this.minutesUntil(lesson);
    if (minutes <= 0) {
      return 'Starting now';
    }
    if (minutes < 60) {
      return `Starts in ${minutes} min`;
    }
    const hours = Math.round(minutes / 60);
    if (hours < 24) {
      return `Starts in ${hours} ${hours === 1 ? 'hour' : 'hours'}`;
    }
    const days = Math.round(hours / 24);
    return `In ${days} ${days === 1 ? 'day' : 'days'}`;
  }
}
