import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { AvailabilitySlot, DAY_NAMES, minutesToTime, timeToMinutes } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';

interface Block {
  start: string;
  end: string;
}

const DISPLAY_ORDER = [1, 2, 3, 4, 5, 6, 0];

@Component({
  selector: 'app-tutor-availability',
  standalone: true,
  imports: [CommonModule, PageHeader],
  templateUrl: './tutor-availability.html',
  styleUrl: './tutor-availability.scss',
})
export class TutorAvailability implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly week = signal<Block[][]>(DAY_NAMES.map(() => []));

  readonly order = DISPLAY_ORDER;
  readonly dayNames = DAY_NAMES;

  readonly problems = computed(() => this.week().map((blocks) => this.validateDay(blocks)));
  readonly hasProblems = computed(() => this.problems().some((p) => p !== null));
  readonly totalHours = computed(() =>
    this.week().reduce(
      (sum, blocks) =>
        sum + blocks.reduce((s, b) => {
          const start = timeToMinutes(b.start);
          const end = timeToMinutes(b.end);
          return start !== null && end !== null && end > start ? s + (end - start) / 60 : s;
        }, 0),
      0
    )
  );

  constructor(private readonly tutorApi: Tutor, private readonly notifications: NotificationService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.tutorApi.getAvailability().subscribe({
      next: (a) => {
        const week: Block[][] = DAY_NAMES.map(() => []);
        for (const slot of a.slots) {
          week[slot.dayOfWeek].push({ start: minutesToTime(slot.startMinute), end: minutesToTime(slot.endMinute) });
        }
        this.week.set(week);
        this.dirty.set(false);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  addBlock(day: number): void {
    this.mutate(day, (blocks) => {
      const last = blocks[blocks.length - 1];
      const lastEnd = last ? timeToMinutes(last.end) : null;
      const start = lastEnd !== null ? Math.min(lastEnd + 60, 22 * 60) : 15 * 60;
      return [...blocks, { start: minutesToTime(start), end: minutesToTime(Math.min(start + 120, 24 * 60)) }];
    });
  }

  removeBlock(day: number, index: number): void {
    this.mutate(day, (blocks) => blocks.filter((_, i) => i !== index));
  }

  setTime(day: number, index: number, field: 'start' | 'end', value: string): void {
    this.mutate(day, (blocks) => blocks.map((b, i) => (i === index ? { ...b, [field]: value } : b)));
  }

  toggleDay(day: number, on: boolean): void {
    if (on) {
      this.addBlock(day);
    } else {
      this.mutate(day, () => []);
    }
  }

  copyToWeekdays(day: number): void {
    const source = this.week()[day].map((b) => ({ ...b }));
    this.week.update((week) => week.map((blocks, d) => (d >= 1 && d <= 5 ? source.map((b) => ({ ...b })) : blocks)));
    this.dirty.set(true);
    this.notifications.showSuccess('Copied', `${DAY_NAMES[day]}’s hours now apply Monday to Friday. Save to keep them.`);
  }

  save(): void {
    this.saveError.set(null);
    if (this.hasProblems()) {
      this.saveError.set('Fix the highlighted days before saving.');
      return;
    }
    const slots: AvailabilitySlot[] = [];
    this.week().forEach((blocks, dayOfWeek) =>
      blocks.forEach((b) => slots.push({ dayOfWeek, startMinute: timeToMinutes(b.start)!, endMinute: timeToMinutes(b.end)! }))
    );

    this.saving.set(true);
    this.tutorApi.updateAvailability(slots).subscribe({
      next: () => {
        this.saving.set(false);
        this.dirty.set(false);
        this.notifications.showSuccess('Availability saved', 'Students can book you in these hours.');
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.saveError.set(err.error?.message ?? 'Your availability wasn’t saved. Try again.');
      },
    });
  }

  private mutate(day: number, change: (blocks: Block[]) => Block[]): void {
    this.week.update((week) => week.map((blocks, d) => (d === day ? change(blocks) : blocks)));
    this.dirty.set(true);
  }

  private validateDay(blocks: Block[]): string | null {
    const parsed = blocks.map((b) => ({ start: timeToMinutes(b.start), end: timeToMinutes(b.end) }));
    if (parsed.some((b) => b.start === null || b.end === null)) {
      return 'Enter a start and end time for each block.';
    }
    if (parsed.some((b) => b.start! % 15 !== 0 || b.end! % 15 !== 0)) {
      return 'Use times on the quarter hour, like 15:00 or 15:15.';
    }
    if (parsed.some((b) => b.end! - b.start! < 30)) {
      return 'Each block must end at least 30 minutes after it starts.';
    }
    const sorted = [...parsed].sort((a, b) => a.start! - b.start!);
    for (let i = 1; i < sorted.length; i++) {
      if (sorted[i].start! < sorted[i - 1].end!) {
        return 'Some blocks overlap.';
      }
    }
    return null;
  }
}
