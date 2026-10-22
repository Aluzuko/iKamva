import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthShell } from '../../components/auth-shell/auth-shell';
import { SIGNUP_ROLES, UserRole } from '../../models/user-role.model';
import { AuthApiService } from '../../services/auth-api.service';

const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

function passwordsMatchValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password && confirmPassword && password !== confirmPassword ? { passwordMismatch: true } : null;
  };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register implements OnInit {
  readonly roles = SIGNUP_ROLES;
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly emailTaken = signal(false);

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authApi: AuthApiService
  ) {
    this.form = this.fb.group(
      {
        role: [UserRole.Student, [Validators.required]],
        fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: passwordsMatchValidator() }
    );
  }

  ngOnInit(): void {
    const fromUrl = this.route.snapshot.paramMap.get('role') as UserRole | null;
    if (fromUrl && this.roles.some((r) => r.role === fromUrl)) {
      this.form.patchValue({ role: fromUrl });
    }

    this.form.get('email')?.valueChanges.subscribe(() => this.emailTaken.set(false));
  }

  invalid(name: 'fullName' | 'email' | 'password'): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  get confirmInvalid(): boolean {
    const c = this.form.get('confirmPassword');
    const touched = !!c && (c.dirty || c.touched);
    return touched && (c!.invalid || this.form.hasError('passwordMismatch'));
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.emailTaken.set(false);
    const { fullName, email, password, role } = this.form.getRawValue();

    this.authApi.register({ fullName: fullName.trim(), email: email.trim(), password, role }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.router.navigate(['/', response.user.role, 'dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.status === 409) {
          this.emailTaken.set(true);
          return;
        }
        this.errorMessage.set(this.messageFor(err));
      },
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    switch (err.status) {
      case 400:
        return 'Some details need fixing. Check the fields and try again.';
      case 403:
        return "That account type can't be created here.";
      case 429:
        return 'Too many attempts. Wait a minute and try again.';
      case 0:
        return "We couldn't reach the server. Check your connection and try again.";
      default:
        return 'Something went wrong while creating your account. Try again.';
    }
  }
}
