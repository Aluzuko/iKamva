import { Component, DestroyRef, OnInit, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { LoadingService } from '../../services/loading.service';

const SHOW_DELAY_MS = 150;

@Component({
  selector: 'app-top-progress',
  standalone: true,
  templateUrl: './top-progress.html',
  styleUrl: './top-progress.scss',
})
export class TopProgress implements OnInit {
  private readonly navigating = signal(false);
  private timer: ReturnType<typeof setTimeout> | null = null;

  readonly busy = computed(() => this.navigating() || this.loading.busy());
  readonly visible = signal(false);

  constructor(
    private readonly router: Router,
    private readonly loading: LoadingService,
    private readonly destroyRef: DestroyRef
  ) {
    effect(() => {
      const busy = this.busy();
      if (busy && !this.timer && !this.visible()) {
        this.timer = setTimeout(() => {
          this.timer = null;
          if (this.busy()) {
            this.visible.set(true);
          }
        }, SHOW_DELAY_MS);
      }
      if (!busy) {
        if (this.timer) {
          clearTimeout(this.timer);
          this.timer = null;
        }
        this.visible.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.navigating.set(true);
      } else if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
        this.navigating.set(false);
      }
    });
  }
}
