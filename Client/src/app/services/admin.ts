import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminBooking,
  AdminDashboard,
  AdminInvoice,
  Analytics,
  CreateSubjectPayload,
  CreateTutorPayload,
  Dispute,
  SubjectPricing,
  SubjectPricingPayload,
  TutorDetail,
  TutorListItem,
  UserAccount,
} from '../models/admin.model';
import { UserRole } from '../models/user-role.model';

@Injectable({
  providedIn: 'root',
})
export class Admin {
  private readonly baseUrl = '/api/admin';

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<AdminDashboard | null> {
    return this.http.get<AdminDashboard | null>(`${this.baseUrl}/dashboard`);
  }

  getTutors(): Observable<TutorListItem[]> {
    return this.http.get<TutorListItem[]>(`${this.baseUrl}/tutors`);
  }

  getVerificationQueue(): Observable<TutorDetail[]> {
    return this.http.get<TutorDetail[]>(`${this.baseUrl}/tutors/verification-queue`);
  }

  getTutor(tutorId: string): Observable<TutorDetail> {
    return this.http.get<TutorDetail>(`${this.baseUrl}/tutors/${tutorId}`);
  }

  createTutor(payload: CreateTutorPayload): Observable<{ tutorId: string }> {
    return this.http.post<{ tutorId: string }>(`${this.baseUrl}/tutors`, payload);
  }

  approveTutor(tutorId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tutors/${tutorId}/approve`, {});
  }

  rejectTutor(tutorId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/tutors/${tutorId}/reject`, { reason });
  }

  getUsers(role?: UserRole): Observable<UserAccount[]> {
    const params = role ? new HttpParams().set('role', role) : undefined;
    return this.http.get<UserAccount[]>(`${this.baseUrl}/users`, { params });
  }

  activateUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/activate`, {});
  }

  suspendUser(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${userId}/suspend`, {});
  }

  getSubjects(): Observable<SubjectPricing[]> {
    return this.http.get<SubjectPricing[]>(`${this.baseUrl}/subjects`);
  }

  createSubject(payload: CreateSubjectPayload): Observable<{ subjectId: string }> {
    return this.http.post<{ subjectId: string }>(`${this.baseUrl}/subjects`, payload);
  }

  updateSubjectPricing(subjectId: string, payload: SubjectPricingPayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/subjects/${subjectId}/pricing`, payload);
  }

  getBookings(): Observable<AdminBooking[]> {
    return this.http.get<AdminBooking[]>(`${this.baseUrl}/bookings`);
  }

  getPayments(): Observable<AdminInvoice[]> {
    return this.http.get<AdminInvoice[]>(`${this.baseUrl}/payments`);
  }

  getDisputes(): Observable<Dispute[]> {
    return this.http.get<Dispute[]>(`${this.baseUrl}/disputes`);
  }

  resolveDispute(disputeId: string, resolution: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/disputes/${disputeId}/resolve`, { resolution });
  }

  rejectDispute(disputeId: string, resolution: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/disputes/${disputeId}/reject`, { resolution });
  }

  getAnalytics(): Observable<Analytics> {
    return this.http.get<Analytics>(`${this.baseUrl}/analytics`);
  }
}
