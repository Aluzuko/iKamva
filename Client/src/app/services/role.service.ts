import { Injectable, computed, signal } from '@angular/core';
import { ROLE_OPTIONS, UserRole } from '../models/user-role.model';

/**
 * Holds the currently active role for the session.
 * In production this would be derived from the authenticated user's
 * claims rather than set manually via the role-select screen.
 */
@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly _currentRole = signal<UserRole | null>(null);
  readonly currentRole = this._currentRole.asReadonly();

  readonly currentRoleOption = computed(() =>
    ROLE_OPTIONS.find((r) => r.role === this._currentRole()) ?? null
  );

  setRole(role: UserRole): void {
    this._currentRole.set(role);
  }
}
