import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ROLE_OPTIONS, UserRole } from '../../models/user-role.model';

@Component({
  selector: 'app-role-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-select.html',
  styleUrl: './role-select.scss',
})
export class RoleSelect {
  readonly roles = ROLE_OPTIONS;

  constructor(private readonly router: Router) {}

  enterAs(role: UserRole): void {
    this.router.navigate(['/login', role]);
  }
  getYear(): number {
    return new Date().getFullYear();
  }
}
