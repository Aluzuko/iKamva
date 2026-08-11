import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { UpcomingLesson } from '../../models/parent.model';
import { Avatar } from '../avatar/avatar';

@Component({
  selector: 'app-lesson-list-item',
  standalone: true,
  imports: [CommonModule, Avatar],
  templateUrl: './lesson-list-item.html',
  styleUrl: './lesson-list-item.scss',
})
export class LessonListItem {
  @Input({ required: true }) lesson!: UpcomingLesson;
  @Input() showStudentName = false;
}
