import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SubjectPricing, TutorDetail, TutorListItem, VerificationStatus } from '../../models/admin.model';
import { Admin } from '../../services/admin';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge } from '../../components/status-badge/status-badge';
import { Dialog } from '../../components/dialog/dialog';
import { Pager, pageOf } from '../../components/pager/pager';
import { TutorReviewCard } from '../../components/tutor-review-card/tutor-review-card';
import { accountView, matchesSearch, statusView } from '../../utils/status';

type Tab = 'queue' | 'all';
type StatusFilter = 'all' | VerificationStatus;

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

@Component({
  selector: 'app-admin-tutors',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeader, Avatar, StatusBadge, Dialog, Pager, TutorReviewCard],
  templateUrl: './admin-tutors.html',
  styleUrl: './admin-tutors.scss',
})
export class AdminTutors implements OnInit {
  readonly tab = signal<Tab>('queue');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly queue = signal<TutorDetail[]>([]);
  readonly tutors = signal<TutorListItem[]>([]);
  readonly busyId = signal<string | null>(null);

  readonly statusFilter = signal<StatusFilter>('all');
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly filtered = computed(() =>
    this.tutors().filter(
      (t) =>
        (this.statusFilter() === 'all' || t.verificationStatus === this.statusFilter()) &&
        matchesSearch(this.search(), t.fullName, t.email, t.subjects.join(' '))
    )
  );
  readonly visible = computed(() => pageOf(this.filtered(), this.page(), this.pageSize));

  readonly rejectTarget = signal<{ tutorId: string; fullName: string; approved: boolean } | null>(null);
  readonly suspendTarget = signal<TutorListItem | null>(null);
  readonly addOpen = signal(false);
  readonly subjects = signal<SubjectPricing[]>([]);
  readonly addError = signal<string | null>(null);
  readonly saving = signal(false);

  readonly rejectForm: FormGroup;
  readonly addForm: FormGroup;

  readonly status = statusView;
  readonly account = accountView;
  readonly filters: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'approved', label: 'Approved' },
    { value: 'pending', label: 'Pending' },
    { value: 'rejected', label: 'Rejected' },
  ];

  constructor(
    private readonly fb: FormBuilder,
    private readonly adminApi: Admin,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]],
    });

    this.addForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      hourlyRate: [null as number | null, [Validators.required, Validators.min(1), Validators.max(10000)]],
      yearsExperience: [0, [Validators.min(0), Validators.max(80)]],
      bio: ['', [Validators.maxLength(2000)]],
      subjectIds: [[] as string[]],
    });
  }

  ngOnInit(): void {
    this.tab.set(this.route.snapshot.queryParamMap.get('tab') === 'all' ? 'all' : 'queue');
    this.load();
  }

  load(): void {
    this.failed.set(false);
    forkJoin({ queue: this.adminApi.getVerificationQueue(), tutors: this.adminApi.getTutors() }).subscribe({
      next: ({ queue, tutors }) => {
        this.queue.set(queue);
        this.tutors.set(tutors);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  setTab(tab: Tab): void {
    this.tab.set(tab);
    this.router.navigate([], { queryParams: { tab: tab === 'all' ? 'all' : null }, replaceUrl: true });
  }

  setFilter(value: StatusFilter): void {
    this.statusFilter.set(value);
    this.page.set(1);
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  countFor(value: StatusFilter): number {
    return value === 'all' ? this.tutors().length : this.tutors().filter((t) => t.verificationStatus === value).length;
  }

  approve(tutor: TutorDetail): void {
    this.busyId.set(tutor.tutorId);
    this.adminApi.approveTutor(tutor.tutorId).subscribe({
      next: () => {
        this.busyId.set(null);
        this.notifications.showSuccess('Tutor approved', `${tutor.fullName} can now be booked by students.`);
        this.load();
      },
      error: () => {
        this.busyId.set(null);
        this.notifications.showError('Approval failed', `${tutor.fullName} couldn't be approved. Try again.`);
      },
    });
  }

  openReject(tutor: { tutorId: string; fullName: string; verificationStatus: VerificationStatus }): void {
    this.rejectForm.reset({ reason: '' });
    this.rejectTarget.set({ tutorId: tutor.tutorId, fullName: tutor.fullName, approved: tutor.verificationStatus === 'approved' });
  }

  confirmReject(): void {
    const target = this.rejectTarget();
    if (!target) {
      return;
    }
    if (this.rejectForm.invalid) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.adminApi.rejectTutor(target.tutorId, this.rejectForm.getRawValue().reason.trim()).subscribe({
      next: () => {
        this.saving.set(false);
        this.rejectTarget.set(null);
        this.notifications.showSuccess(
          target.approved ? 'Approval revoked' : 'Verification rejected',
          `${target.fullName} will see your reason.`
        );
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.notifications.showError('Couldn’t save', 'The tutor’s status wasn’t changed. Try again.');
      },
    });
  }

  toggleActive(tutor: TutorListItem): void {
    if (tutor.isActive) {
      this.suspendTarget.set(tutor);
      return;
    }
    this.setActive(tutor, true);
  }

  setActive(tutor: TutorListItem, active: boolean): void {
    this.saving.set(true);
    const call = active ? this.adminApi.activateUser(tutor.tutorId) : this.adminApi.suspendUser(tutor.tutorId);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.suspendTarget.set(null);
        this.notifications.showSuccess(
          active ? 'Account reactivated' : 'Account suspended',
          active ? `${tutor.fullName} can sign in again.` : `${tutor.fullName} can no longer sign in.`
        );
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.notifications.showError('Couldn’t update the account', 'Try again.');
      },
    });
  }

  openAdd(): void {
    this.addForm.reset({ fullName: '', email: '', password: '', hourlyRate: null, yearsExperience: 0, bio: '', subjectIds: [] });
    this.addError.set(null);
    this.addOpen.set(true);
    if (!this.subjects().length) {
      this.adminApi.getSubjects().subscribe({ next: (s) => this.subjects.set(s) });
    }
  }

  isSubjectSelected(id: string): boolean {
    return (this.addForm.get('subjectIds')?.value as string[]).includes(id);
  }

  toggleSubject(id: string): void {
    const current = this.addForm.get('subjectIds')?.value as string[];
    this.addForm.patchValue({ subjectIds: current.includes(id) ? current.filter((s) => s !== id) : [...current, id] });
  }

  invalid(name: string): boolean {
    const c = this.addForm.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  get rejectInvalid(): boolean {
    const c = this.rejectForm.get('reason');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  submitAdd(): void {
    this.addError.set(null);
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const v = this.addForm.getRawValue();
    this.saving.set(true);
    this.adminApi
      .createTutor({
        fullName: v.fullName.trim(),
        email: v.email.trim(),
        password: v.password,
        hourlyRate: Number(v.hourlyRate),
        yearsExperience: Number(v.yearsExperience) || 0,
        bio: v.bio?.trim() || null,
        subjectIds: v.subjectIds,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.addOpen.set(false);
          this.notifications.showSuccess('Tutor added', `${v.fullName.trim()} is approved and can sign in with the password you set.`);
          this.setTab('all');
          this.load();
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.addError.set(
            err.status === 409
              ? 'An account with this email already exists.'
              : err.status === 400
                ? 'Some details need fixing. Check the fields and try again.'
                : 'The tutor couldn’t be added. Try again.'
          );
        },
      });
  }
}
