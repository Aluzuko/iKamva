import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ROLE_OPTIONS, UserRole } from '../../models/user-role.model';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './auth-shell.html',
  styleUrl: './auth-shell.scss',
})
export class AuthShell {
  @Input() role: UserRole | null = null;
  @Input({ required: true }) title!: string;
  @Input() subtitle: string | null = null;

  get roleOption() {
    return this.role ? ROLE_OPTIONS.find((r) => r.role === this.role) ?? null : null;
  }
}
