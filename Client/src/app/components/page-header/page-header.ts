import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UserRole } from '../../models/user-role.model';
import { Topbar } from '../topbar/topbar';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink, Topbar],
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeader {
  @Input({ required: true }) title!: string;
  @Input() subtitle: string | null = null;
  @Input() backLink: string | unknown[] | null = null;
  @Input() backLabel = 'Back';

  constructor(private readonly route: ActivatedRoute) {}

  get role(): UserRole {
    let current: ActivatedRoute | null = this.route;
    while (current) {
      const role = current.snapshot.data['role'] as UserRole | undefined;
      if (role) {
        return role;
      }
      current = current.parent;
    }
    return UserRole.Student;
  }
}
