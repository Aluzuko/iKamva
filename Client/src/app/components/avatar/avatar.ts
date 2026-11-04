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

  private readonly palette = ['#9a3a32', '#3a5a6a', '#5f4f86', '#2d7356', '#a8692a', '#4c5c37', '#7a3e5c'];

  get initials(): string {
    const parts = this.name.replace(/^Dr\.?\s+/i, '').trim().split(/\s+/);
    const first = parts[0]?.[0] ?? '';
    const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
    return (first + last).toUpperCase();
  }

  get color(): string {
    const hash = [...this.name].reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
    return this.palette[hash % this.palette.length];
  }
}
