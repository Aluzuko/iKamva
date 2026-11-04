import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { UserAccount } from '../../models/admin.model';
import { UserRole, roleLabel } from '../../models/user-role.model';
import { Admin } from '../../services/admin';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { StatCard } from '../../components/stat-card/stat-card';
import { Dialog } from '../../components/dialog/dialog';
import { Pager, pageOf } from '../../components/pager/pager';
import { accountView, matchesSearch } from '../../utils/status';

type RoleFilter = 'all' | UserRole.Student | UserRole.Parent;
type AccountFilter = 'any' | 'active' | 'suspended';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, PageHeader, Avatar, StatusBadge, StatCard, Dialog, Pager],
  templateUrl: './admin-users.html',
})
export class AdminUsers implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly users = signal<UserAccount[]>([]);
  readonly roleFilter = signal<RoleFilter>('all');
  readonly accountFilter = signal<AccountFilter>('any');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly saving = signal(false);
  readonly suspendTarget = signal<UserAccount | null>(null);

  readonly people = computed(() => this.users().filter((u) => u.role === UserRole.Student || u.role === UserRole.Parent));
  readonly filtered = computed(() =>
    this.people().filter(
      (u) =>
        (this.roleFilter() === 'all' || u.role === this.roleFilter()) &&
        (this.accountFilter() === 'any' || (this.accountFilter() === 'active') === u.isActive) &&
        matchesSearch(this.search(), u.fullName, u.email)
    )
  );
  readonly visible = computed(() => pageOf(this.filtered(), this.page(), this.pageSize));
  readonly studentCount = computed(() => this.people().filter((u) => u.role === UserRole.Student).length);
  readonly parentCount = computed(() => this.people().filter((u) => u.role === UserRole.Parent).length);
  readonly suspendedCount = computed(() => this.people().filter((u) => !u.isActive).length);

  readonly roleFilters: { value: RoleFilter; label: string }[] = [
    { value: 'all', label: 'Everyone' },
    { value: UserRole.Student, label: 'Students' },
    { value: UserRole.Parent, label: 'Parents' },
  ];
  readonly accountFilters: { value: AccountFilter; label: string }[] = [
    { value: 'any', label: 'Any status' },
    { value: 'active', label: 'Active' },
    { value: 'suspended', label: 'Suspended' },
  ];
  readonly account = accountView;
  readonly roleName = roleLabel;

  constructor(private readonly adminApi: Admin, private readonly notifications: NotificationService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.adminApi.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  setRole(value: RoleFilter): void {
    this.roleFilter.set(value);
    this.page.set(1);
  }

  setAccount(value: AccountFilter): void {
    this.accountFilter.set(value);
    this.page.set(1);
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  roleCount(value: RoleFilter): number {
    return value === 'all' ? this.people().length : this.people().filter((u) => u.role === value).length;
  }

  toggle(user: UserAccount): void {
    if (user.isActive) {
      this.suspendTarget.set(user);
      return;
    }
    this.setActive(user, true);
  }

  setActive(user: UserAccount, active: boolean): void {
    this.saving.set(true);
    const call = active ? this.adminApi.activateUser(user.userId) : this.adminApi.suspendUser(user.userId);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.suspendTarget.set(null);
        this.users.update((list) => list.map((u) => (u.userId === user.userId ? { ...u, isActive: active } : u)));
        this.notifications.showSuccess(
          active ? 'Account reactivated' : 'Account suspended',
          active ? `${user.fullName} can sign in again.` : `${user.fullName} can no longer sign in.`
        );
      },
      error: () => {
        this.saving.set(false);
        this.notifications.showError('Couldn’t update the account', 'Try again.');
      },
    });
  }
}
