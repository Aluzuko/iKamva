import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { LessonHistoryItem as LessonHistoryItemModel } from '../../models/parent.model';
import { Avatar } from '../avatar/avatar';
import { BadgeVariant, StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'app-lesson-history-item',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge],
  templateUrl: './lesson-history-item.html',
  styleUrl: './lesson-history-item.scss',
})
export class LessonHistoryItem {
  @Input({ required: true }) item!: LessonHistoryItemModel;

  get statusVariant(): BadgeVariant {
    switch (this.item.status) {
      case 'completed':
        return 'success';
      case 'cancelled':
        return 'danger';
      default:
        return 'neutral';
    }
  }

  get statusLabel(): string {
    return this.item.status.charAt(0).toUpperCase() + this.item.status.slice(1);
  }
}
