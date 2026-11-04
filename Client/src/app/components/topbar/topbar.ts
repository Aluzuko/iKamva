import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, Input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../../services/notification.service';
import { SessionService } from '../../services/session.service';
import { UserRole } from '../../models/user-role.model';
import { Avatar } from '../avatar/avatar';

type Panel = 'notifications' | 'account' | null;

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterLink, Avatar],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
})
export class Topbar {
  @Input({ required: true }) role!: UserRole;
  @Input() variant: 'full' | 'avatar' = 'full';

  readonly open = signal<Panel>(null);

  constructor(
    readonly notificationService: NotificationService,
    readonly session: SessionService,
    private readonly host: ElementRef<HTMLElement>
  ) {}

  get userName(): string {
    return this.session.user?.fullName ?? '';
  }

  toggle(panel: Exclude<Panel, null>): void {
    this.open.update((current) => (current === panel ? null : panel));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(null);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(null);
  }
}
