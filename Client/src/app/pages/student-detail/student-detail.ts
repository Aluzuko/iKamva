import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StudentDetail } from '../../models/parent.model';
import { ParentApiService } from '../../services/parent-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { LessonHistoryItem } from '../../components/lesson-history-item/lesson-history-item';
import { LessonListItem } from '../../components/lesson-list-item/lesson-list-item';
import { StatCard } from '../../components/stat-card/stat-card';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { SubjectBadge } from '../../components/subject-badge/subject-badge';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    Avatar,
    LessonHistoryItem,
    LessonListItem,
    StatCard,
    StatusBadge,
    SubjectBadge,
  ],
  templateUrl: './student-detail.html',
  styleUrl: './student-detail.scss',
})
export class StudentDetailPage implements OnInit {
  readonly student = signal<StudentDetail | null>(null);
  readonly loading = signal(true);

  constructor(private readonly route: ActivatedRoute, private readonly parentApi: ParentApiService) {}

  ngOnInit(): void {
    const studentId = this.route.snapshot.paramMap.get('studentId')!;
    this.parentApi.getChildDetail(studentId).subscribe((data) => {
      this.student.set(data);
      this.loading.set(false);
    });
  }
}
