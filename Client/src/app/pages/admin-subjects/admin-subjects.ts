import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { SubjectPricing } from '../../models/admin.model';
import { AdminApiService } from '../../services/admin-api.service';

@Component({
  selector: 'app-admin-subjects',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './admin-subjects.html',
  styleUrl: './admin-subjects.scss',
})
export class AdminSubjects implements OnInit {
  readonly subjects = signal<SubjectPricing[]>([]);
  readonly loading = signal(true);
  readonly editingId = signal<string | null>(null);
  readonly showCreateForm = signal(false);
  readonly submitting = signal(false);

  editRate = 0;
  editCommission = 0;

  readonly createForm: FormGroup;

  constructor(private readonly fb: FormBuilder, private readonly adminApi: AdminApiService) {
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      defaultHourlyRate: [70, [Validators.required, Validators.min(1)]],
      commissionPercent: [15, [Validators.required, Validators.min(0), Validators.max(100)]],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.adminApi.getSubjects().subscribe((subjects) => {
      this.subjects.set(subjects);
      this.loading.set(false);
    });
  }

  startEdit(subject: SubjectPricing): void {
    this.editingId.set(subject.subjectId);
    this.editRate = subject.defaultHourlyRate;
    this.editCommission = subject.commissionPercent;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(subjectId: string): void {
    this.adminApi.updateSubjectPricing(subjectId, this.editRate, this.editCommission).subscribe(() => {
      this.editingId.set(null);
      this.load();
    });
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { name, defaultHourlyRate, commissionPercent } = this.createForm.getRawValue();
    this.adminApi.createSubject(name, defaultHourlyRate, commissionPercent).subscribe(() => {
      this.submitting.set(false);
      this.showCreateForm.set(false);
      this.createForm.reset({ defaultHourlyRate: 70, commissionPercent: 15 });
      this.load();
    });
  }
}
