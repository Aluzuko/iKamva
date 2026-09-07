import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SubjectPricing, TutorDetail, TutorListItem } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { BadgeVariant, StatusBadge } from '../../components/status-badge/status-badge';
import { SubjectBadge } from '../../components/subject-badge/subject-badge';

type Tab = 'all' | 'queue';

@Component({
  selector: 'app-admin-tutors',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Avatar, StatusBadge, SubjectBadge],
  templateUrl: './admin-tutors.html',
  styleUrl: './admin-tutors.scss',
})
export class AdminTutors implements OnInit {
  readonly activeTab = signal<Tab>('all');
  readonly loading = signal(true);
  readonly tutors = signal<TutorListItem[]>([]);
  readonly queue = signal<TutorDetail[]>([]);
  readonly subjects = signal<SubjectPricing[]>([]);

  readonly showCreateForm = signal(false);
  readonly submitting = signal(false);
  readonly rejectingTutorId = signal<string | null>(null);
  readonly rejectReason = signal('');
  readonly previewDocument = signal<{ fileName: string; fileUrl: string } | null>(null);

  readonly createForm: FormGroup;

  constructor(private readonly fb: FormBuilder, private readonly adminApi: AdminApiService) {
    this.createForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      hourlyRate: [80, [Validators.required, Validators.min(1)]],
      yearsExperience: [0, [Validators.required, Validators.min(0)]],
      bio: [''],
      subjectIds: this.fb.array([]),
    });
  }

  ngOnInit(): void {
    this.loadAll();
    this.adminApi.getSubjects().subscribe((subjects) => this.subjects.set(subjects));
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  loadAll(): void {
    this.loading.set(true);
    this.adminApi.getTutors().subscribe((tutors) => {
      this.tutors.set(tutors);
      this.loading.set(false);
    });
    this.adminApi.getVerificationQueue().subscribe((queue) => this.queue.set(queue));
  }

  statusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'approved':
        return 'success';
      case 'rejected':
        return 'danger';
      default:
        return 'warning';
    }
  }

  statusLabel(status: string): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  toggleSubject(subjectId: string, checked: boolean): void {
    const current: string[] = (this.createForm.value.subjectIds as string[]) ?? [];
    const next = checked ? [...current, subjectId] : current.filter((id) => id !== subjectId);
    this.createForm.patchValue({ subjectIds: next });
  }

  isSubjectChecked(subjectId: string): boolean {
    const current: string[] = (this.createForm.value.subjectIds as string[]) ?? [];
    return current.includes(subjectId);
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.adminApi.createTutor(this.createForm.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showCreateForm.set(false);
        this.createForm.reset({ hourlyRate: 80, yearsExperience: 0, subjectIds: [] });
        this.loadAll();
      },
      error: () => this.submitting.set(false),
    });
  }

  approve(tutorId: string): void {
    this.adminApi.approveTutor(tutorId).subscribe(() => this.loadAll());
  }

  startReject(tutorId: string): void {
    this.rejectingTutorId.set(tutorId);
    this.rejectReason.set('');
  }

  cancelReject(): void {
    this.rejectingTutorId.set(null);
  }

  confirmReject(): void {
    const tutorId = this.rejectingTutorId();
    if (!tutorId || !this.rejectReason().trim()) {
      return;
    }
    this.adminApi.rejectTutor(tutorId, this.rejectReason().trim()).subscribe(() => {
      this.rejectingTutorId.set(null);
      this.loadAll();
    });
  }

  openPreview(fileName: string, fileUrl: string): void {
    this.previewDocument.set({ fileName, fileUrl });
  }

  closePreview(): void {
    this.previewDocument.set(null);
  }
}
