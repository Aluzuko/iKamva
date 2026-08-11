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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthShell } from '../../components/auth-shell/auth-shell';
import { ROLE_OPTIONS, UserRole } from '../../models/user-role.model';
import { AuthApiService } from '../../services/auth-api.service';

function passwordsMatchValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password && confirmPassword && password !== confirmPassword
      ? { passwordMismatch: true }
      : null;
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
  role: UserRole = UserRole.Student;
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authApi: AuthApiService
  ) {
    this.form = this.fb.group(
      {
        fullName: ['', [Validators.required, Validators.minLength(2)]],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]],
      },
      { validators: passwordsMatchValidator() }
    );
  }

  ngOnInit(): void {
    this.role = (this.route.snapshot.paramMap.get('role') as UserRole) ?? UserRole.Student;
  }

  get roleLabel(): string {
    return ROLE_OPTIONS.find((r) => r.role === this.role)?.label ?? 'Mentivio';
  }

  fieldInvalid(name: 'fullName' | 'email' | 'password'): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  get confirmPasswordInvalid(): boolean {
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
    const { fullName, email, password } = this.form.getRawValue();

    this.authApi.register({ fullName, email, password, role: this.role }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.router.navigate(['/login', this.role], { queryParams: { registered: '1' } });
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(
          err.status === 409 ? 'An account with this email already exists.' : 'Something went wrong. Try again.'
        );
      },
    });
  }

  continueWith(provider: 'google' | 'facebook'): void {
    // TODO: wire up real OAuth (Google/Facebook) once backend supports it.
    this.router.navigate(['/', this.role, 'dashboard']);
  }
}
