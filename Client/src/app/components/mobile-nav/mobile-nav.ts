import { CommonModule } from '@angular/common';
import { Component, HostListener, Input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs';
import { ROLE_NAV_ITEMS, shortNavLabel } from '../../config/role-nav.config';
import { NavItem } from '../../models/nav-item.model';
import { UserRole } from '../../models/user-role.model';
import { Messages } from '../../services/messages';

const MAX_TABS = 5;

@Component({
  selector: 'app-mobile-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './mobile-nav.html',
  styleUrl: './mobile-nav.scss',
})
export class MobileNav {
  @Input({ required: true }) role!: UserRole;

  readonly moreOpen = signal(false);
  readonly shortLabel = shortNavLabel;

  constructor(private readonly router: Router, private readonly messages: Messages) {
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.moreOpen.set(false));
  }

  get items(): NavItem[] {
    return ROLE_NAV_ITEMS[this.role] ?? [];
  }

  get tabs(): NavItem[] {
    return this.items.length > MAX_TABS ? this.items.slice(0, MAX_TABS - 1) : this.items;
  }

  get overflow(): NavItem[] {
    return this.items.length > MAX_TABS ? this.items.slice(MAX_TABS - 1) : [];
  }

  badgeFor(item: NavItem): number | 'new' | null {
    if (item.route === 'messages') {
      return this.messages.unread() || null;
    }
    return item.badge ?? null;
  }

  get overflowBadge(): boolean {
    return this.overflow.some((item) => !!this.badgeFor(item));
  }

  get overflowActive(): boolean {
    return this.overflow.some((item) => this.router.url.startsWith(`/${this.role}/${item.route}`));
  }

  @HostListener('document:keydown.escape')
  close(): void {
    this.moreOpen.set(false);
  }
}
