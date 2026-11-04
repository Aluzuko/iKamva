import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { Profile as ProfileData } from '../../models/profile.model';
import { ProfileApiService } from '../../services/profile-api.service';
import { AuthApiService } from '../../services/auth-api.service';
import { Avatar } from '../../components/avatar/avatar';
import { StatusBadge, BadgeVariant } from '../../components/status-badge/status-badge';
import { SubjectBadge } from '../../components/subject-badge/subject-badge';

function passwordsMatchValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const newPassword = group.get('newPassword')?.value;
    const confirmNewPassword = group.get('confirmNewPassword')?.value;
    return newPassword && confirmNewPassword && newPassword !== confirmNewPassword
      ? { passwordMismatch: true }
      : null;
  };
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Avatar, StatusBadge, SubjectBadge],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  readonly profile = signal<ProfileData | null>(null);
  readonly loading = signal(true);

  readonly editingPersonal = signal(false);
  readonly savingPersonal = signal(false);

  readonly editingEmail = signal(false);
  readonly savingEmail = signal(false);
  readonly emailError = signal<string | null>(null);

  readonly editingRoleInfo = signal(false);
  readonly savingRoleInfo = signal(false);

  readonly changingPassword = signal(false);
  readonly savingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordSuccess = signal(false);

  readonly signingOutAll = signal(false);

  readonly linkCodeCopied = signal(false);

  readonly personalForm: FormGroup;
  readonly emailForm: FormGroup;
  readonly roleInfoForm: FormGroup;
  readonly passwordForm: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly profileApi: ProfileApiService,
    private readonly authApi: AuthApiService,
    private readonly router: Router
  ) {
    this.personalForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
    });

    this.emailForm = this.fb.group({
      newEmail: ['', [Validators.required, Validators.email]],
      currentPassword: ['', [Validators.required]],
    });

    this.roleInfoForm = this.fb.group({
      bio: [''],
      hourlyRate: [0],
      yearLevel: [''],
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmNewPassword: ['', [Validators.required]],
      },
      { validators: passwordsMatchValidator() }
    );
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.profileApi.getProfile().subscribe((data) => {
      this.profile.set(data);
      this.personalForm.patchValue({ fullName: data.fullName });
      this.roleInfoForm.patchValue({
        bio: data.bio ?? '',
        hourlyRate: data.hourlyRate ?? 0,
        yearLevel: data.yearLevel ?? '',
      });
      this.loading.set(false);
    });
  }

  savePersonal(): void {
    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }

    this.savingPersonal.set(true);
    this.profileApi.updateProfile({ fullName: this.personalForm.getRawValue().fullName }).subscribe(() => {
      this.savingPersonal.set(false);
      this.editingPersonal.set(false);
      this.load();
    });
  }

  toggleEmailForm(): void {
    this.editingEmail.set(!this.editingEmail());
    this.emailError.set(null);
    this.emailForm.reset();
  }

  submitEmailChange(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.savingEmail.set(true);
    this.emailError.set(null);
    const { newEmail, currentPassword } = this.emailForm.getRawValue();

    this.profileApi.changeEmail(newEmail, currentPassword).subscribe({
      next: () => {
        this.savingEmail.set(false);
        this.editingEmail.set(false);
        this.load();
      },
      error: (err) => {
        this.savingEmail.set(false);
        this.emailError.set(
          err.status === 409 ? 'That email is already in use.' : 'Current password is incorrect.'
        );
      },
    });
  }

  toggleRoleInfoEdit(): void {
    this.editingRoleInfo.set(!this.editingRoleInfo());
    const data = this.profile();
    if (data) {
      this.roleInfoForm.patchValue({
        bio: data.bio ?? '',
        hourlyRate: data.hourlyRate ?? 0,
        yearLevel: data.yearLevel ?? '',
      });
    }
  }

  saveRoleInfo(): void {
    const data = this.profile();
    if (!data) {
      return;
    }

    this.savingRoleInfo.set(true);
    this.profileApi.updateProfile({ fullName: data.fullName, ...this.roleInfoForm.getRawValue() }).subscribe(() => {
      this.savingRoleInfo.set(false);
      this.editingRoleInfo.set(false);
      this.load();
    });
  }

  togglePasswordForm(): void {
    this.changingPassword.set(!this.changingPassword());
    this.passwordError.set(null);
    this.passwordSuccess.set(false);
    this.passwordForm.reset();
  }

  submitPasswordChange(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.savingPassword.set(true);
    this.passwordError.set(null);
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.profileApi.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.savingPassword.set(false);
        this.passwordSuccess.set(true);
        this.passwordForm.reset();
      },
      error: (err) => {
        this.savingPassword.set(false);
        this.passwordError.set(
          err.status === 400 ? 'Current password is incorrect.' : 'Something went wrong. Try again.'
        );
      },
    });
  }

  signOutAllDevices(): void {
    this.signingOutAll.set(true);
    this.authApi.logoutAll().subscribe(() => {
      this.router.navigate(['/role-select']);
    });
  }

  copyLinkCode(code: string): void {
    navigator.clipboard.writeText(code).then(() => {
      this.linkCodeCopied.set(true);
      setTimeout(() => this.linkCodeCopied.set(false), 2000);
    });
  }

  verificationVariant(status: string): BadgeVariant {
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
}
