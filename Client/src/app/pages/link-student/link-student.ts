import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ParentApiService } from '../../services/parent-api.service';
import { LinkRelationship } from '../../models/parent.model';

type Tab = 'invite' | 'code';

@Component({
  selector: 'app-link-student',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './link-student.html',
  styleUrl: './link-student.scss',
})
export class LinkStudent {
  readonly activeTab = signal<Tab>('invite');
  readonly submitted = signal(false);
  readonly submitting = signal(false);

  readonly inviteForm: FormGroup;
  readonly codeForm: FormGroup;

  constructor(private readonly fb: FormBuilder, private readonly parentApi: ParentApiService) {
    this.inviteForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      relationship: ['parent' as LinkRelationship, [Validators.required]],
    });

    this.codeForm = this.fb.group({
      code: ['', [Validators.required]],
    });
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.submitted.set(false);
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { email, relationship } = this.inviteForm.getRawValue();
    this.parentApi.inviteStudent(email!, relationship as LinkRelationship).subscribe(() => {
      this.submitting.set(false);
      this.submitted.set(true);
      this.inviteForm.reset({ email: '', relationship: 'parent' });
    });
  }

  submitCode(): void {
    if (this.codeForm.invalid) {
      this.codeForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.parentApi.linkByCode(this.codeForm.getRawValue().code!).subscribe(() => {
      this.submitting.set(false);
      this.submitted.set(true);
      this.codeForm.reset({ code: '' });
    });
  }
}