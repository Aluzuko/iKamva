import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { DOCUMENT_TYPE_LABELS, DocumentType } from '../../models/admin.model';
import { MAX_DOCUMENT_BYTES, SubjectOption, TutorDocument, TutorProfile as Profile, verificationLabel } from '../../models/tutor.model';
import { Tutor } from '../../services/tutor';
import { FileOpener } from '../../services/file-opener';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { Dialog } from '../../components/dialog/dialog';

interface DocumentSlot {
  type: DocumentType;
  label: string;
  hint: string;
}

const ACCEPTED_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];

@Component({
  selector: 'app-tutor-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader, StatusBadge, Dialog],
  templateUrl: './tutor-profile.html',
  styleUrl: './tutor-profile.scss',
})
export class TutorProfile implements OnInit {
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly profile = signal<Profile | null>(null);
  readonly subjects = signal<SubjectOption[]>([]);
  readonly saving = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly uploading = signal<DocumentType | null>(null);
  readonly documentErrors = signal<Partial<Record<DocumentType, string>>>({});
  readonly removeTarget = signal<TutorDocument | null>(null);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly state = computed(() => verificationLabel(this.profile()?.verificationState ?? 'notSubmitted'));
  readonly canSubmit = computed(() => {
    const p = this.profile();
    return !!p && p.canEditDocuments && p.missingForSubmission.length === 0;
  });

  readonly slots: DocumentSlot[] = [
    { type: 'idDocument', label: DOCUMENT_TYPE_LABELS.idDocument, hint: 'South African ID, smart ID card or passport.' },
    { type: 'qualification', label: DOCUMENT_TYPE_LABELS.qualification, hint: 'Degree, diploma or teaching certificate.' },
    { type: 'backgroundCheck', label: DOCUMENT_TYPE_LABELS.backgroundCheck, hint: 'Police clearance certificate issued in the last 6 months.' },
  ];

  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly tutorApi: Tutor,
    private readonly files: FileOpener,
    private readonly notifications: NotificationService
  ) {
    this.form = this.fb.group({
      hourlyRate: [null as number | null, [Validators.required, Validators.min(1), Validators.max(10000)]],
      yearsExperience: [0, [Validators.required, Validators.min(0), Validators.max(80)]],
      bio: ['', [Validators.maxLength(2000)]],
      subjectIds: [[] as string[], [Validators.required, Validators.minLength(1)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed.set(false);
    forkJoin({ profile: this.tutorApi.getProfile(), subjects: this.tutorApi.getSubjects() }).subscribe({
      next: ({ profile, subjects }) => {
        this.subjects.set(subjects);
        this.apply(profile);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  documentFor(type: DocumentType): TutorDocument | undefined {
    return this.profile()?.documents.find((d) => d.documentType === type);
  }

  isSubjectSelected(id: string): boolean {
    return (this.form.get('subjectIds')?.value as string[]).includes(id);
  }

  toggleSubject(id: string): void {
    const control = this.form.get('subjectIds')!;
    const current = control.value as string[];
    control.setValue(current.includes(id) ? current.filter((s) => s !== id) : [...current, id]);
    control.markAsTouched();
    control.markAsDirty();
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  saveProfile(): void {
    this.profileError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.tutorApi
      .updateProfile({
        hourlyRate: Number(v.hourlyRate),
        yearsExperience: Number(v.yearsExperience) || 0,
        bio: (v.bio as string)?.trim() || null,
        subjectIds: v.subjectIds,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.form.markAsPristine();
          this.notifications.showSuccess('Profile saved', 'Your teaching details are up to date.');
          this.refresh();
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.profileError.set(err.error?.message ?? 'Your profile wasn’t saved. Try again.');
        },
      });
  }

  onFileChosen(type: DocumentType, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    const problem = !ACCEPTED_TYPES.includes(file.type)
      ? 'Upload a PDF, JPG or PNG file.'
      : file.size > MAX_DOCUMENT_BYTES
        ? 'Files must be 5 MB or smaller.'
        : null;
    this.setDocumentError(type, problem);
    if (problem) {
      return;
    }

    this.uploading.set(type);
    this.tutorApi.uploadDocument(type, file).subscribe({
      next: () => {
        this.uploading.set(null);
        this.notifications.showSuccess('Document uploaded', `${DOCUMENT_TYPE_LABELS[type]} saved.`);
        this.refresh();
      },
      error: (err: HttpErrorResponse) => {
        this.uploading.set(null);
        this.setDocumentError(
          type,
          err.status === 413 ? 'Files must be 5 MB or smaller.' : err.error?.message ?? 'The upload failed. Try again.'
        );
      },
    });
  }

  view(doc: TutorDocument): void {
    this.files.open(this.tutorApi.documentUrl(doc.documentId), doc.fileName);
  }

  confirmRemove(): void {
    const doc = this.removeTarget();
    if (!doc) {
      return;
    }
    this.tutorApi.deleteDocument(doc.documentId).subscribe({
      next: () => {
        this.removeTarget.set(null);
        this.notifications.showSuccess('Document removed', `${DOCUMENT_TYPE_LABELS[doc.documentType]} removed.`);
        this.refresh();
      },
      error: (err: HttpErrorResponse) => {
        this.removeTarget.set(null);
        this.notifications.showError('Couldn’t remove the document', err.error?.message ?? 'Try again.');
      },
    });
  }

  submit(): void {
    this.submitError.set(null);
    if (this.form.dirty) {
      this.submitError.set('Save your profile changes before submitting.');
      return;
    }
    this.submitting.set(true);
    this.tutorApi.submitForReview().subscribe({
      next: () => {
        this.submitting.set(false);
        this.notifications.showSuccess('Submitted for review', 'We’ll let you know once your documents have been checked.');
        this.refresh();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.submitError.set(err.error?.message ?? 'We couldn’t submit your profile. Try again.');
      },
    });
  }

  formatSize(bytes: number): string {
    return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  private refresh(): void {
    this.tutorApi.getProfile().subscribe({ next: (p) => this.apply(p, false) });
  }

  private apply(profile: Profile, resetForm = true): void {
    this.profile.set(profile);
    if (resetForm || this.form.pristine) {
      this.form.reset({
        hourlyRate: profile.hourlyRate,
        yearsExperience: profile.yearsExperience,
        bio: profile.bio ?? '',
        subjectIds: [...profile.subjectIds],
      });
    }
  }

  private setDocumentError(type: DocumentType, message: string | null): void {
    this.documentErrors.update((errors) => {
      const next = { ...errors };
      if (message) {
        next[type] = message;
      } else {
        delete next[type];
      }
      return next;
    });
  }
}
