import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  Output,
  ViewChild,
} from '@angular/core';

let nextId = 0;

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dialog.html',
  styleUrl: './dialog.scss',
})
export class Dialog implements AfterViewInit, OnDestroy {
  @Input({ required: true }) title!: string;
  @Input() description: string | null = null;
  @Input() wide = false;
  @Output() closed = new EventEmitter<void>();

  @ViewChild('panel', { static: true }) panel!: ElementRef<HTMLElement>;

  readonly titleId = `dialog-title-${++nextId}`;
  private readonly previouslyFocused = document.activeElement as HTMLElement | null;

  ngAfterViewInit(): void {
    const body = this.panel.nativeElement.querySelector<HTMLElement>('.dlg-body');
    const target =
      body?.querySelector<HTMLElement>('input, select, textarea') ??
      this.panel.nativeElement.querySelector<HTMLElement>('.dlg-foot ' + FOCUSABLE) ??
      this.panel.nativeElement;
    setTimeout(() => target.focus());
  }

  ngOnDestroy(): void {
    this.previouslyFocused?.focus?.();
  }

  close(): void {
    this.closed.emit();
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.stopPropagation();
      this.close();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const items = Array.from(this.panel.nativeElement.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
      (el) => el.offsetParent !== null
    );
    if (!items.length) {
      return;
    }

    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
}
