import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [CommonModule],
  template: `<span class="stars" role="img" [attr.aria-label]="label"><i *ngFor="let s of icons" class="bi" [ngClass]="s" aria-hidden="true"></i></span>`,
  styles: [`:host { display: inline-flex; } .stars { display: inline-flex; gap: 0.0625rem; color: var(--tl-accent); font-size: 0.8125rem; }`],
})
export class StarRating {
  @Input({ required: true }) rating!: number;
  @Input() max = 5;

  get icons(): string[] {
    return Array.from({ length: this.max }, (_, i) =>
      this.rating >= i + 1 ? 'bi-star-fill' : this.rating >= i + 0.5 ? 'bi-star-half' : 'bi-star'
    );
  }

  get label(): string {
    return `Rated ${this.rating} out of ${this.max}`;
  }
}
