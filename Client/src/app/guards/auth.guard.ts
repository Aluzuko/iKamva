import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../services/token-storage.service';
import { UserRole } from '../models/user-role.model';

export const authGuard: CanActivateFn = () => {
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  if (tokenStorage.isAuthenticated) {
    return true;
  }

  return router.createUrlTree(['/role-select']);
};

export const roleGuard: CanActivateFn = (route) => {
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  const requiredRole = route.data['role'] as UserRole;
  const user = tokenStorage.currentUser;

  if (user && user.role === requiredRole) {
    return true;
  }

  return router.createUrlTree(['/role-select']);
};
