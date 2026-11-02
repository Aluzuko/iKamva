import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DOCUMENT_TYPE_LABELS, TutorDetail, VerificationDocument } from '../../models/admin.model';
import { FileOpener } from '../../services/file-opener';
import { Avatar } from '../avatar/avatar';
import { StatusBadge } from '../status-badge/status-badge';
import { SubjectBadge } from '../subject-badge/subject-badge';
import { statusView } from '../../utils/status';

@Component({
  selector: 'app-tutor-review-card',
  standalone: true,
  imports: [CommonModule, RouterLink, Avatar, StatusBadge, SubjectBadge],
  templateUrl: './tutor-review-card.html',
  styleUrl: './tutor-review-card.scss',
})
export class TutorReviewCard {
  @Input({ required: true }) tutor!: TutorDetail;
  @Input() busy = false;
  @Input() showDetailLink = true;
  @Output() approve = new EventEmitter<TutorDetail>();
  @Output() reject = new EventEmitter<TutorDetail>();

  readonly docLabels = DOCUMENT_TYPE_LABELS;
  readonly status = statusView;

  constructor(private readonly files: FileOpener) {}

  openDocument(doc: VerificationDocument): void {
    if (/^https?:\/\//i.test(doc.fileUrl)) {
      window.open(doc.fileUrl, '_blank', 'noopener');
      return;
    }
    this.files.open(doc.fileUrl, doc.fileName);
  }
}
