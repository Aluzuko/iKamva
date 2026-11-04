import { HttpContextToken } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';

export const SILENT_REQUEST = new HttpContextToken<boolean>(() => false);

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly active = signal(0);

  readonly busy = computed(() => this.active() > 0);

  start(): void {
    this.active.update((n) => n + 1);
  }

  stop(): void {
    this.active.update((n) => Math.max(0, n - 1));
  }
}
