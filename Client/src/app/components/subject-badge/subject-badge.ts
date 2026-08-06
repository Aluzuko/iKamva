import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-subject-badge',
  standalone: true,
  templateUrl: './subject-badge.html',
  styleUrl: './subject-badge.scss',
})
export class SubjectBadge {
  @Input({ required: true }) name!: string;
}
