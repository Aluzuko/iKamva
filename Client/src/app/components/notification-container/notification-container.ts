import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NotificationService, NotificationType } from '../../services/notification.service';

@Component({
  selector: 'app-notification-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-container.html',
  styleUrl: './notification-container.scss',
})
export class NotificationContainer {
  constructor(public readonly notificationService: NotificationService) {}

  iconFor(type: NotificationType): string {
    switch (type) {
      case 'success':
        return 'bi-check-circle';
      case 'warning':
        return 'bi-exclamation-triangle';
      case 'danger':
        return 'bi-x-circle';
      default:
        return 'bi-info-circle';
    }
  }

  dismiss(id: string): void {
    this.notificationService.dismissToast(id);
  }
}
