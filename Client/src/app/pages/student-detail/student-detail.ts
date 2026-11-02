import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StudentDetail } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { LessonHistoryItem } from '../../components/lesson-history-item/lesson-history-item';
import { LessonListItem } from '../../components/lesson-list-item/lesson-list-item';
import { StatCard } from '../../components/stat-card/stat-card';
import { SubjectBadge } from '../../components/subject-badge/subject-badge';
import { PageHeader } from '../../components/page-header/page-header';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LessonHistoryItem, LessonListItem, StatCard, SubjectBadge, PageHeader],
  templateUrl: './student-detail.html',
  styleUrl: './student-detail.scss',
})
export class StudentDetailPage implements OnInit {
  readonly student = signal<StudentDetail | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);

  constructor(private readonly route: ActivatedRoute, private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    const studentId = this.route.snapshot.paramMap.get('studentId')!;
    this.parentApi.getChildDetail(studentId).subscribe({
      next: (data) => {
        this.student.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      },
    });
  }

  get subtitle(): string | null {
    const s = this.student();
    if (!s) {
      return null;
    }
    return [s.yearLevel, s.isActive ? 'Linked to your account' : 'Account inactive'].filter(Boolean).join(' · ');
  }
}
