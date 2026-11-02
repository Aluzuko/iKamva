import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BookingCreated,
  CreateBookingPayload,
  Slots,
  StudentBilling,
  StudentDashboard,
  StudentLesson,
  SubjectRef,
  TutorCard,
  TutorPublicProfile,
  TutorSearch,
} from '../models/student.model';

@Injectable({
  providedIn: 'root',
})
export class Student {
  private readonly baseUrl = '/api/student';

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<StudentDashboard> {
    return this.http.get<StudentDashboard>(`${this.baseUrl}/dashboard`);
  }

  getSubjects(): Observable<SubjectRef[]> {
    return this.http.get<SubjectRef[]>(`${this.baseUrl}/subjects`);
  }

  searchTutors(search: TutorSearch): Observable<TutorCard[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(search)) {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<TutorCard[]>(`${this.baseUrl}/tutors`, { params });
  }

  getTutor(tutorId: string): Observable<TutorPublicProfile> {
    return this.http.get<TutorPublicProfile>(`${this.baseUrl}/tutors/${tutorId}`);
  }

  getSlots(tutorId: string, duration: number, from?: string, days = 14): Observable<Slots> {
    let params = new HttpParams().set('duration', duration).set('days', days);
    if (from) {
      params = params.set('from', from);
    }
    return this.http.get<Slots>(`${this.baseUrl}/tutors/${tutorId}/slots`, { params });
  }

  book(payload: CreateBookingPayload): Observable<BookingCreated> {
    return this.http.post<BookingCreated>(`${this.baseUrl}/bookings`, payload);
  }

  getLessons(): Observable<StudentLesson[]> {
    return this.http.get<StudentLesson[]>(`${this.baseUrl}/lessons`);
  }

  cancelLesson(bookingId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/lessons/${bookingId}/cancel`, { reason });
  }

  reviewLesson(bookingId: string, rating: number, comment: string | null): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/lessons/${bookingId}/review`, { rating, comment });
  }

  getBilling(): Observable<StudentBilling> {
    return this.http.get<StudentBilling>(`${this.baseUrl}/billing`);
  }
}
