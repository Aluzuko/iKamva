import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TutorLesson } from '../../models/tutor.model';
import { Avatar } from '../avatar/avatar';
import { StatusBadge } from '../status-badge/status-badge';
import { statusView } from '../../utils/status';

@Component({
  selector: 'app-tutor-lesson-row',
  standalone: true,
  imports: [CommonModule, RouterLink, Avatar, StatusBadge],
  templateUrl: './tutor-lesson-row.html',
  styleUrl: './tutor-lesson-row.scss',
})
export class TutorLessonRow {
  @Input({ required: true }) lesson!: TutorLesson;
  @Input() actions = true;
  @Input() busy = false;
  @Input() allowCancel = true;
  @Output() cancel = new EventEmitter<TutorLesson>();
  @Output() complete = new EventEmitter<TutorLesson>();

  get badge(): { text: string; variant: ReturnType<typeof statusView>['variant'] } {
    return this.lesson.needsCompletion ? { text: 'To confirm', variant: 'warning' } : statusView(this.lesson.status);
  }

  get canCancel(): boolean {
    return this.allowCancel && this.lesson.status === 'upcoming' && !this.lesson.needsCompletion && new Date(this.lesson.scheduledAt).getTime() > Date.now();
  }
}
