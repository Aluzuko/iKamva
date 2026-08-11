import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChildSummary } from '../../models/parent.model';
import { Avatar } from '../avatar/avatar';
import { SubjectBadge } from '../subject-badge/subject-badge';

@Component({
  selector: 'app-child-summary-card',
  standalone: true,
  imports: [CommonModule, RouterLink, Avatar, SubjectBadge],
  templateUrl: './child-summary-card.html',
  styleUrl: './child-summary-card.scss',
})
export class ChildSummaryCard {
  @Input({ required: true }) child!: ChildSummary;
}
