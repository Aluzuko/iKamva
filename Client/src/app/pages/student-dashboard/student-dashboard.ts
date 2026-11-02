import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StudentDashboard as Dashboard, StudentLesson } from '../../models/student.model';
import { Student } from '../../services/student';
import { SessionService } from '../../services/session.service';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatCard } from '../../components/stat-card/stat-card';
import { Avatar } from '../../components/avatar/avatar';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeader, StatCard, Avatar],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.scss',
})
export class StudentDashboard implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly dashboard = signal<Dashboard | null>(null);
  readonly copied = signal(false);

  constructor(
    private readonly studentApi: Student,
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
    this.studentApi.getDashboard().subscribe({
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

  startsIn(lesson: StudentLesson): string {
    const minutes = Math.round((new Date(lesson.scheduledAt).getTime() - Date.now()) / 60_000);
    if (minutes <= 0) {
      return 'Happening now';
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

  maxLessons(d: Dashboard): number {
    return Math.max(1, ...d.subjects.map((s) => s.completedLessons));
  }

  copyCode(code: string): void {
    const done = () => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    };
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(code).then(done, () => this.notifications.showError('Couldn’t copy', 'Select the code and copy it yourself.'));
    } else {
      this.notifications.showError('Couldn’t copy', 'Select the code and copy it yourself.');
    }
  }
}
