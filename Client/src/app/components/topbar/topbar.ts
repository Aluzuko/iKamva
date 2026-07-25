import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { UserRole } from '../../models/user-role.model';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
})
export class Topbar {
  @Input({ required: true }) role!: UserRole;
  @Output() toggleSidebar = new EventEmitter<void>();

  constructor(public readonly notificationService: NotificationService) {}
}
