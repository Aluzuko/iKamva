import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ROLE_NAV_ITEMS } from '../../config/role-nav.config';
import { ROLE_OPTIONS, UserRole } from '../../models/user-role.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {
  @Input({ required: true }) role!: UserRole;
  @Input() collapsed = false;

  get navItems() {
    return ROLE_NAV_ITEMS[this.role] ?? [];
  }

  get roleOption() {
    return ROLE_OPTIONS.find((r) => r.role === this.role) ?? null;
  }
}
