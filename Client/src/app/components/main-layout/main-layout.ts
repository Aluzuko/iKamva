import { CommonModule } from '@angular/common';
import { Component, HostBinding, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { RoleService } from '../../services/role.service';
import { UserRole } from '../../models/user-role.model';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, Sidebar, Topbar],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout implements OnInit {
  readonly sidebarCollapsed = signal(false);
  role: UserRole = UserRole.Student;

  // Applies e.g. "role-student" to the host element so child components
  // (via var(--tl-role-color)) can theme themselves per active role.
  @HostBinding('class') get hostClass(): string {
    return `app-shell role-${this.role}`;
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly roleService: RoleService
  ) {}

  ngOnInit(): void {
    const role = this.route.snapshot.data['role'] as UserRole;
    this.role = role;
    this.roleService.setRole(role);
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update((v) => !v);
  }
}
