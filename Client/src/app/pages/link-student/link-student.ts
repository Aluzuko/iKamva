import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ParentApiService } from '../../services/parent-api.service';
import { LinkRelationship } from '../../models/parent.model';
import { PageHeader } from '../../components/page-header/page-header';

type Result = { kind: 'success' | 'error'; text: string } | null;

@Component({
  selector: 'app-link-student',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeader],
  templateUrl: './link-student.html',
  styleUrl: './link-student.scss',
})
export class LinkStudent {
  readonly inviteForm: FormGroup;
  readonly codeForm: FormGroup;
  readonly inviting = signal(false);
  readonly linking = signal(false);
  readonly inviteResult = signal<Result>(null);
  readonly codeResult = signal<Result>(null);

  constructor(private readonly fb: FormBuilder, private readonly parentApi: ParentApiService) {
    this.inviteForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      relationship: ['parent' as LinkRelationship, [Validators.required]],
    });

    this.codeForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{8}$/)]],
    });
  }

  invalid(form: FormGroup, name: string): boolean {
    const c = form.get(name);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    this.inviting.set(true);
    this.inviteResult.set(null);
    const { email, relationship } = this.inviteForm.getRawValue();
    this.parentApi.inviteStudent(email.trim(), relationship as LinkRelationship).subscribe({
      next: () => {
        this.inviting.set(false);
        this.inviteResult.set({
          kind: 'success',
          text: `Invite sent to ${email.trim()}. They'll be linked once they accept or sign up with that email.`,
        });
        this.inviteForm.reset({ email: '', relationship: 'parent' });
      },
      error: () => {
        this.inviting.set(false);
        this.inviteResult.set({ kind: 'error', text: "The invite couldn't be sent. Try again." });
      },
    });
  }

  submitCode(): void {
    if (this.codeForm.invalid) {
      this.codeForm.markAllAsTouched();
      return;
    }

    this.linking.set(true);
    this.codeResult.set(null);
    const code = (this.codeForm.getRawValue().code as string).trim().toUpperCase();
    this.parentApi.linkByCode(code).subscribe({
      next: () => {
        this.linking.set(false);
        this.codeResult.set({ kind: 'success', text: 'Student linked. You can now see them under My Children.' });
        this.codeForm.reset({ code: '' });
      },
      error: (err: HttpErrorResponse) => {
        this.linking.set(false);
        this.codeResult.set({
          kind: 'error',
          text:
            err.status === 404
              ? "No student found with that code. Check it on your child's Profile page."
              : "The student couldn't be linked. Try again.",
        });
      },
    });
  }
}
