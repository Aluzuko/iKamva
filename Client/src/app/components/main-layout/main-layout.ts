import { CommonModule } from '@angular/common';
import { Component, HostBinding, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink, RouterOutlet } from '@angular/router';
import { UserRole, roleLabel } from '../../models/user-role.model';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';
import { MobileNav } from '../mobile-nav/mobile-nav';
import { Messages } from '../../services/messages';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, Sidebar, Topbar, MobileNav],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout implements OnInit, OnDestroy {
  role: UserRole = UserRole.Student;

  @HostBinding('class') get hostClass(): string {
    return `role-${this.role}`;
  }

  constructor(private readonly route: ActivatedRoute, private readonly messages: Messages) {}

  ngOnInit(): void {
    this.role = this.route.snapshot.data['role'] as UserRole;
    if (this.role !== UserRole.Admin) {
      this.messages.startUnreadPolling();
    }
  }

  ngOnDestroy(): void {
    this.messages.stopUnreadPolling();
  }

  get roleName(): string {
    return roleLabel(this.role);
  }
}
