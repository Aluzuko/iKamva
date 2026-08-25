import { Injectable } from '@angular/core';
import { UserSummary } from '../models/auth.model';

interface StoredSession {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserSummary;
}

const STORAGE_KEY = 'iKamva.session';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  save(session: StoredSession): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }

  get(): StoredSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredSession) : null;
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
  }

  get accessToken(): string | null {
    return this.get()?.accessToken ?? null;
  }

  get currentUser(): UserSummary | null {
    return this.get()?.user ?? null;
  }

  get isAuthenticated(): boolean {
    const session = this.get();
    if (!session) {
      return false;
    }
    return new Date(session.accessTokenExpiresAt).getTime() > Date.now();
  }
}
