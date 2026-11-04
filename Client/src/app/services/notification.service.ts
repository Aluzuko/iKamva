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

const TOAST_MS = 4500;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _notifications = signal<AppNotification[]>([]);
  private readonly _toasts = signal<string[]>([]);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = computed(() => this._notifications().filter((n) => !n.read).length);
  readonly toasts = computed(() => this._notifications().filter((n) => this._toasts().includes(n.id)));

  push(type: NotificationType, title: string, message: string, durationMs = TOAST_MS): void {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    const time = new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
    this._notifications.update((list) => [{ id, title, message, time, read: false, type }, ...list]);
    this._toasts.update((list) => [...list, id]);
    setTimeout(() => this.dismissToast(id), durationMs);
  }

  showSuccess(title: string, message: string, durationMs = TOAST_MS): void {
    this.push('success', title, message, durationMs);
  }

  showError(title: string, message: string, durationMs = TOAST_MS): void {
    this.push('danger', title, message, durationMs);
  }

  dismissToast(id: string): void {
    this._toasts.update((list) => list.filter((t) => t !== id));
  }

  markAsRead(id: string): void {
    this._notifications.update((list) => list.map((n) => (n.id === id ? { ...n, read: true } : n)));
  }

  markAllAsRead(): void {
    this._notifications.update((list) => list.map((n) => ({ ...n, read: true })));
  }
}
