import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { UpcomingLesson } from '../../models/parent.model';
import { Avatar } from '../avatar/avatar';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-lesson-list-item',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge],
  templateUrl: './lesson-list-item.html',
  styleUrl: './lesson-list-item.scss',
})
export class LessonListItem {
  @Input({ required: true }) lesson!: UpcomingLesson;
  @Input() showStudentName = false;
}
