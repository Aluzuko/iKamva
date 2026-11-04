import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pager',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pager.html',
  styleUrl: './pager.scss',
})
export class Pager {
  @Input({ required: true }) total!: number;
  @Input({ required: true }) page!: number;
  @Input() pageSize = 20;
  @Input() noun = 'items';
  @Output() pageChange = new EventEmitter<number>();

  get pageCount(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  get from(): number {
    return this.total ? (this.page - 1) * this.pageSize + 1 : 0;
  }

  get to(): number {
    return Math.min(this.page * this.pageSize, this.total);
  }

  go(page: number): void {
    if (page >= 1 && page <= this.pageCount && page !== this.page) {
      this.pageChange.emit(page);
    }
  }
}

export function pageOf<T>(items: T[], page: number, pageSize = 20): T[] {
  return items.slice((page - 1) * pageSize, page * pageSize);
}
