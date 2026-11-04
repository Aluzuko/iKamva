import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthShell } from '../../components/auth-shell/auth-shell';
import { AuthApiService } from '../../services/auth-api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  readonly form: FormGroup;
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    private readonly authApi: AuthApiService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
    });
  }

  invalid(name: 'email' | 'password'): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    const { email, password } = this.form.getRawValue();

    this.authApi.login({ email: email.trim(), password }).subscribe({
      next: (response) => {
        this.submitting.set(false);
        this.router.navigate(['/', response.user.role, 'dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.form.get('password')?.reset();
        this.errorMessage.set(this.messageFor(err));
      },
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    switch (err.status) {
      case 401:
        return 'The email or password is incorrect.';
      case 403:
        return 'This account has been suspended. Contact support if you think this is a mistake.';
      case 429:
        return 'Too many sign-in attempts. Wait a minute and try again.';
      case 0:
        return "We couldn't reach the server. Check your connection and try again.";
      default:
        return 'Something went wrong while signing in. Try again.';
    }
  }
}
