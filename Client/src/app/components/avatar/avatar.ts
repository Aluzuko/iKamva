import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-avatar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './avatar.html',
  styleUrl: './avatar.scss',
})
export class Avatar {
  @Input({ required: true }) name!: string;
  @Input() size: 'sm' | 'md' | 'lg' = 'md';

  private readonly palette = ['#2563eb', '#0d9488', '#f59e0b', '#dc2626', '#7c3aed', '#0891b2'];

  get initials(): string {
    const parts = this.name.trim().split(/\s+/);
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
    return (first + last).toUpperCase();
  }

  get color(): string {
    const hash = [...this.name].reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return this.palette[hash % this.palette.length];
  }
}
