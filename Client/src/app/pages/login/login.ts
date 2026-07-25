import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthShell } from '../../components/auth-shell/auth-shell';
import { ROLE_OPTIONS, UserRole } from '../../models/user-role.model';
import { Api } from '../../services/api';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  role: UserRole = UserRole.Student;
  justRegistered = false;
  readonly form: FormGroup;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly api: Api,
    private readonly notifications: NotificationService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  ngOnInit(): void {
    this.role = (this.route.snapshot.paramMap.get('role') as UserRole) ?? UserRole.Student;
    this.justRegistered = this.route.snapshot.queryParamMap.get('registered') === '1';
  }

  get roleLabel(): string {
    return ROLE_OPTIONS.find((r) => r.role === this.role)?.label ?? 'Mentivio';
  }

  get emailInvalid(): boolean {
    const c = this.form.get('email');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  get passwordInvalid(): boolean {
    const c = this.form.get('password');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.api.post('/api/auth/login', this.form.value).subscribe({
      next: (result: any) => {
        debugger;
        console.log('Login successful:', result);
        if (result && !result?.status) {
          this.notifications.showSuccess('Signed in successfully', 'Welcome back.');
          this.router.navigate(['/', this.role, 'dashboard']);
        }
      },
      error: (err) => {
        const message = err?.error?.message || 'Please check your email and password and try again.';
        this.notifications.showError('Sign-in failed', message);
        console.error('Login failed:', err);
      },
    });
  
  }

  continueWith(provider: 'google' | 'facebook'): void {
    // TODO: wire up real OAuth (Google/Facebook) once backend supports it.
    // For now this mirrors a successful social sign-in for prototype purposes.
    this.router.navigate(['/', this.role, 'dashboard']);
  }
}