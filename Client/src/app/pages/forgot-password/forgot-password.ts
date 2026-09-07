import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthShell } from '../../components/auth-shell/auth-shell';
import { UserRole } from '../../models/user-role.model';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AuthShell],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword implements OnInit {
  /** Optional — only present if the user arrived from a role-specific login page. */
  role: UserRole | null = null;
  readonly submitted = signal(false);
  readonly form: FormGroup;

  constructor(private readonly fb: FormBuilder, private readonly route: ActivatedRoute) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  ngOnInit(): void {
    const roleParam = this.route.snapshot.queryParamMap.get('role');
    this.role = roleParam ? (roleParam as UserRole) : null;
  }

  get emailInvalid(): boolean {
    const c = this.form.get('email');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  get loginLink(): (string | UserRole)[] {
    return this.role ? ['/login', this.role] : ['/role-select'];
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitted.set(true);
  }
}
