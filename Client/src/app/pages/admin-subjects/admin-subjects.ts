import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SubjectPricing } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Dialog } from '../../components/dialog/dialog';
import { matchesSearch } from '../../utils/status';

@Component({
  selector: 'app-admin-subjects',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, Dialog],
  templateUrl: './admin-subjects.html',
})
export class AdminSubjects implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly subjects = signal<SubjectPricing[]>([]);
  readonly search = signal('');
  readonly editing = signal<SubjectPricing | null>(null);
  readonly adding = signal(false);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);

  readonly filtered = computed(() =>
    [...this.subjects()]
      .filter((s) => matchesSearch(this.search(), s.name))
      .sort((a, b) => a.name.localeCompare(b.name))
  );

  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly adminApi: Admin,
    private readonly notifications: NotificationService
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      defaultHourlyRate: [null as number | null, [Validators.required, Validators.min(0), Validators.max(10000)]],
      commissionPercent: [null as number | null, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    this.adminApi.getSubjects().subscribe({
      next: (s) => {
        this.subjects.set(s);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  tutorReceives(rate: number, commission: number): number {
    return Math.round(rate * (100 - commission)) / 100;
  }

  get preview(): number | null {
    const v = this.form.getRawValue();
    const rate = Number(v.defaultHourlyRate);
    const commission = Number(v.commissionPercent);
    if (v.defaultHourlyRate === null || v.commissionPercent === null || isNaN(rate) || isNaN(commission)) {
      return null;
    }
    if (commission < 0 || commission > 100 || rate < 0) {
      return null;
    }
    return this.tutorReceives(rate, commission);
  }

  openEdit(subject: SubjectPricing): void {
    this.formError.set(null);
    this.form.get('name')?.disable();
    this.form.reset({
      name: subject.name,
      defaultHourlyRate: subject.defaultHourlyRate,
      commissionPercent: subject.commissionPercent,
    });
    this.editing.set(subject);
  }

  openAdd(): void {
    this.formError.set(null);
    this.form.get('name')?.enable();
    this.form.reset({ name: '', defaultHourlyRate: null, commissionPercent: 15 });
    this.adding.set(true);
  }

  close(): void {
    this.editing.set(null);
    this.adding.set(false);
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.enabled && c.invalid && (c.dirty || c.touched);
  }

  submit(): void {
    this.formError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const pricing = { defaultHourlyRate: Number(v.defaultHourlyRate), commissionPercent: Number(v.commissionPercent) };
    const editing = this.editing();
    this.saving.set(true);

    if (editing) {
      this.adminApi.updateSubjectPricing(editing.subjectId, pricing).subscribe({
        next: () => {
          this.saving.set(false);
          this.subjects.update((list) => list.map((s) => (s.subjectId === editing.subjectId ? { ...s, ...pricing } : s)));
          this.close();
          this.notifications.showSuccess('Pricing updated', `${editing.name} pricing applies to new bookings.`);
        },
        error: () => this.fail(null),
      });
      return;
    }

    const name = (v.name as string).trim();
    this.adminApi.createSubject({ name, ...pricing }).subscribe({
      next: ({ subjectId }) => {
        this.saving.set(false);
        this.subjects.update((list) => [...list, { subjectId, name, ...pricing, tutorCount: 0 }]);
        this.close();
        this.notifications.showSuccess('Subject added', `Tutors can now offer ${name}.`);
      },
      error: (err: HttpErrorResponse) => this.fail(err),
    });
  }

  private fail(err: HttpErrorResponse | null): void {
    this.saving.set(false);
    this.formError.set(
      err?.status === 409 ? 'A subject with this name already exists.' : 'The changes weren’t saved. Try again.'
    );
  }
}
