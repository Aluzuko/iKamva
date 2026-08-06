import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ChildSummary,
  FamilyDashboard,
  LinkRelationship,
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

  payInvoice(invoiceId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/payments/${invoiceId}/pay`, {});
  }

  inviteStudent(email: string, relationship: LinkRelationship): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/link-student/invite`, { email, relationship });
  }

  linkByCode(code: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/link-student/code`, { code });
  }
}
