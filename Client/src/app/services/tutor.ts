import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DocumentType } from '../models/admin.model';
import {
  Availability,
  AvailabilitySlot,
  Earnings,
  Feedback,
  PayoutRecord,
  SubjectOption,
  TutorDashboard,
  TutorDocument,
  TutorLesson,
  TutorProfile,
  UpdateTutorProfilePayload,
} from '../models/tutor.model';

@Injectable({
  providedIn: 'root',
})
export class Tutor {
  private readonly baseUrl = '/api/tutor';

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<TutorDashboard> {
    return this.http.get<TutorDashboard>(`${this.baseUrl}/dashboard`);
  }

  getProfile(): Observable<TutorProfile> {
    return this.http.get<TutorProfile>(`${this.baseUrl}/profile`);
  }

  updateProfile(payload: UpdateTutorProfilePayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/profile`, payload);
  }

  getSubjects(): Observable<SubjectOption[]> {
    return this.http.get<SubjectOption[]>(`${this.baseUrl}/subjects`);
  }

  uploadDocument(documentType: DocumentType, file: File): Observable<TutorDocument> {
    const form = new FormData();
    form.append('documentType', documentType);
    form.append('file', file, file.name);
    return this.http.post<TutorDocument>(`${this.baseUrl}/documents`, form);
  }

  deleteDocument(documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/documents/${documentId}`);
  }

  documentUrl(documentId: string): string {
    return `${this.baseUrl}/documents/${documentId}/file`;
  }

  submitForReview(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/verification/submit`, {});
  }

  getAvailability(): Observable<Availability> {
    return this.http.get<Availability>(`${this.baseUrl}/availability`);
  }

  updateAvailability(slots: AvailabilitySlot[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/availability`, { slots });
  }

  getLessons(): Observable<TutorLesson[]> {
    return this.http.get<TutorLesson[]>(`${this.baseUrl}/lessons`);
  }

  cancelLesson(bookingId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/lessons/${bookingId}/cancel`, { reason });
  }

  completeLesson(bookingId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/lessons/${bookingId}/complete`, {});
  }

  updateNotes(bookingId: string, notes: string, recordingUrl: string | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/lessons/${bookingId}/notes`, { notes, recordingUrl });
  }

  getFeedback(): Observable<Feedback> {
    return this.http.get<Feedback>(`${this.baseUrl}/feedback`);
  }

  getEarnings(): Observable<Earnings> {
    return this.http.get<Earnings>(`${this.baseUrl}/earnings`);
  }

  requestPayout(): Observable<PayoutRecord> {
    return this.http.post<PayoutRecord>(`${this.baseUrl}/payouts`, {});
  }
}
