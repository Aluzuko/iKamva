import { Injectable, computed, signal } from '@angular/core';

export type NotificationType = 'info' | 'success' | 'warning' | 'danger';

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  time: string;
  read: boolean;
  type: NotificationType;
}

/**
 * Mock notification store shared across all roles. Replace the seed data
 * with a real API call (e.g. GET /api/notifications) when the backend
 * endpoint is ready — the signal-based interface here won't need to change.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _notifications = signal<AppNotification[]>([
    {
      id: '1',
      title: 'Lesson reminder',
      message: 'Algebra II with Sarah Chen starts in 30 minutes',
      time: '30m ago',
      read: false,
      type: 'info',
    },
    {
      id: '2',
      title: 'Payment received',
      message: 'Invoice #INV-1042 has been paid',
      time: '2h ago',
      read: false,
      type: 'success',
    },
    {
      id: '3',
      title: 'Document expiring',
      message: 'Your background check expires in 14 days',
      time: '1d ago',
      read: true,
      type: 'warning',
    },
  ]);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = computed(
    () => this._notifications().filter((n) => !n.read).length
  );

  markAsRead(id: string): void {
    this._notifications.update((list) =>
      list.map((n) => (n.id === id ? { ...n, read: true } : n))
    );
  }

  markAllAsRead(): void {
    this._notifications.update((list) => list.map((n) => ({ ...n, read: true })));
  }
}
