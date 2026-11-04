import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TutorDetail } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Dialog } from '../../components/dialog/dialog';
import { TutorReviewCard } from '../../components/tutor-review-card/tutor-review-card';

@Component({
  selector: 'app-admin-tutor-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, Dialog, TutorReviewCard],
  templateUrl: './admin-tutor-detail.html',
})
export class AdminTutorDetail implements OnInit {
  readonly tutor = signal<TutorDetail | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly notFound = signal(false);
  readonly busy = signal(false);
  readonly rejectOpen = signal(false);
  readonly rejectForm: FormGroup;

  private tutorId = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly adminApi: Admin,
    private readonly notifications: NotificationService
  ) {
    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
    });
  }

  ngOnInit(): void {
    this.tutorId = this.route.snapshot.paramMap.get('tutorId') ?? '';
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.notFound.set(false);
    this.adminApi.getTutor(this.tutorId).subscribe({
      next: (t) => {
        this.tutor.set(t);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.failed.set(true);
        }
      },
    });
  }

  approve(): void {
    const t = this.tutor();
    if (!t) {
      return;
    }
    this.busy.set(true);
    this.adminApi.approveTutor(t.tutorId).subscribe({
      next: () => {
        this.busy.set(false);
        this.notifications.showSuccess('Tutor approved', `${t.fullName} can now be booked by students.`);
        this.load();
      },
      error: () => {
        this.busy.set(false);
        this.notifications.showError('Approval failed', 'Try again.');
      },
    });
  }

  openReject(): void {
    this.rejectForm.reset({ reason: '' });
    this.rejectOpen.set(true);
  }

  get rejectInvalid(): boolean {
    const c = this.rejectForm.get('reason');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  confirmReject(): void {
    const t = this.tutor();
    if (!t) {
      return;
    }
    if (this.rejectForm.invalid) {
      this.rejectForm.markAllAsTouched();
      return;
    }
    const wasApproved = t.verificationStatus === 'approved';
    this.busy.set(true);
    this.adminApi.rejectTutor(t.tutorId, this.rejectForm.getRawValue().reason.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.rejectOpen.set(false);
        this.notifications.showSuccess(wasApproved ? 'Approval revoked' : 'Verification rejected', `${t.fullName} will see your reason.`);
        this.load();
      },
      error: () => {
        this.busy.set(false);
        this.notifications.showError('Couldn’t save', 'The tutor’s status wasn’t changed. Try again.');
      },
    });
  }
}
