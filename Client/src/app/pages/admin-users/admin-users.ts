import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ManagedUserRole, UserAccount } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';

type FilterOption = 'all' | ManagedUserRole;

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, Avatar, StatusBadge],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.scss',
})
export class AdminUsers implements OnInit {
  readonly users = signal<UserAccount[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<FilterOption>('all');

  constructor(private readonly adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.load();
  }

  setFilter(filter: FilterOption): void {
    this.filter.set(filter);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const current = this.filter();
    const role = current === 'all' ? undefined : current;
    this.adminApi.getUsers(role).subscribe((users) => {
      this.users.set(users);
      this.loading.set(false);
    });
  }

  toggleActive(user: UserAccount): void {
    const action = user.isActive ? this.adminApi.suspendUser(user.userId) : this.adminApi.activateUser(user.userId);
    action.subscribe(() => this.load());
  }
}