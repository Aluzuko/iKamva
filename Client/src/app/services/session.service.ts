import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApiService } from './auth-api.service';
import { TokenStorageService } from './token-storage.service';
import { UserSummary } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class SessionService {
  constructor(
    private readonly authApi: AuthApiService,
    private readonly tokenStorage: TokenStorageService,
    private readonly router: Router
  ) {}

  get user(): UserSummary | null {
    return this.tokenStorage.currentUser;
  }

  get firstName(): string {
    const name = this.user?.fullName?.replace(/^Dr\.?\s+/i, '') ?? '';
    return name.split(/\s+/)[0] ?? '';
  }

  homeFor(user: UserSummary | null = this.user): string {
    return user ? `/${user.role}/dashboard` : '/login';
  }

  signOut(): void {
    const done = () => this.router.navigate(['/login']);
    this.authApi.logout().subscribe({ error: done, complete: done });
  }
}
