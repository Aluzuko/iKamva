import { Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { SILENT_REQUEST } from './loading.service';
import { Observable } from 'rxjs';
import {
  ChildSummary,
  FamilyDashboard,
  LinkRelationship,
  PaymentAttempt,
  PaymentRedirect,
  PaymentsOverview,
  StudentDetail,
} from '../models/parent.model';

@Injectable({ providedIn: 'root' })
export class ParentApiService {
  private readonly baseUrl = '/api/parent';

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<FamilyDashboard> {
    return this.http.get<FamilyDashboard>(`${this.baseUrl}/dashboard`);
  }

  getChildren(): Observable<ChildSummary[]> {
    return this.http.get<ChildSummary[]>(`${this.baseUrl}/children`);
  }

  getChildDetail(studentId: string): Observable<StudentDetail> {
    return this.http.get<StudentDetail>(`${this.baseUrl}/children/${studentId}`);
  }

  getPayments(): Observable<PaymentsOverview> {
    return this.http.get<PaymentsOverview>(`${this.baseUrl}/payments`);
  }

  startCheckout(invoiceId: string): Observable<PaymentRedirect> {
    return this.http.post<PaymentRedirect>(`${this.baseUrl}/payments/${invoiceId}/checkout`, {});
  }

  getPaymentAttempt(attemptId: string, silent = false): Observable<PaymentAttempt> {
    return this.http.get<PaymentAttempt>(
      `${this.baseUrl}/payments/attempts/${attemptId}`,
      silent ? { context: new HttpContext().set(SILENT_REQUEST, true) } : {}
    );
  }

  inviteStudent(email: string, relationship: LinkRelationship): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/link-student/invite`, { email, relationship });
  }

  linkByCode(code: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/link-student/code`, { code });
  }
}
