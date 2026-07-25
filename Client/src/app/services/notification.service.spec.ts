import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    service = new NotificationService();
  });

  it('shows a toast and removes it after its duration', () => {
    vi.useFakeTimers();

    service.showError('Sign-in failed', 'Please try again.', 1000);

    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].title).toBe('Sign-in failed');

    vi.advanceTimersByTime(1000);

    expect(service.toasts().length).toBe(0);

    vi.useRealTimers();
  });
});
