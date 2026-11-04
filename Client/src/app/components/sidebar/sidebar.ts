import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ROLE_NAV_ITEMS } from '../../config/role-nav.config';
import { UserRole, roleLabel } from '../../models/user-role.model';
import { SessionService } from '../../services/session.service';
import { Avatar } from '../avatar/avatar';
import { Messages } from '../../services/messages';
import { NavItem } from '../../models/nav-item.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, Avatar],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {
  @Input({ required: true }) role!: UserRole;

  constructor(readonly session: SessionService, private readonly messages: Messages) {}

  badgeFor(item: NavItem): number | 'new' | null {
    if (item.route === 'messages') {
      return this.messages.unread() || null;
    }
    return item.badge ?? null;
  }

  get navItems() {
    return ROLE_NAV_ITEMS[this.role] ?? [];
  }

  get roleName(): string {
    return roleLabel(this.role);
  }

  get userName(): string {
    return this.session.user?.fullName ?? '';
  }
}
