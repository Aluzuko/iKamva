import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminBooking,
  AdminDashboard,
  AdminInvoice,
  Analytics,
  CreateTutorPayload,
  Dispute,
  ManagedUserRole,
  SubjectPricing,
  TutorDetail,
  TutorListItem,
  UserAccount,
} from '../models/admin.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${this.baseUrl}/dashboard`);
  }

  getTutors(): Observable<TutorListItem[]> {
    return this.http.get<TutorListItem[]>(`${this.baseUrl}/tutors`);
  }

  getVerificationQueue(): Observable<TutorDetail[]> {
    return this.http.get<TutorDetail[]>(`${this.baseUrl}/tutors/verification-queue`);
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

  getUsers(role?: ManagedUserRole): Observable<UserAccount[]> {
    let params = new HttpParams();
    if (role) {
      params = params.set('role', role);
    }
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

  createSubject(name: string, defaultHourlyRate: number, commissionPercent: number): Observable<{ subjectId: string }> {
    return this.http.post<{ subjectId: string }>(`${this.baseUrl}/subjects`, {
      name,
      defaultHourlyRate,
      commissionPercent,
    });
  }

  updateSubjectPricing(subjectId: string, defaultHourlyRate: number, commissionPercent: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/subjects/${subjectId}/pricing`, {
      defaultHourlyRate,
      commissionPercent,
    });
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
