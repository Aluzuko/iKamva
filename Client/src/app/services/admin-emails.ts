import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EmailCategory, EmailDetail, EmailPage, EmailProcessResult, EmailStats, EmailStatus } from '../models/email.model';

export interface EmailQuery {
  status?: EmailStatus | null;
  category?: EmailCategory | null;
  search?: string | null;
  page: number;
  pageSize: number;
}

export interface ComposeLetter {
  toEmail: string;
  toName: string | null;
  subject: string;
  message: string;
  files: File[];
}

@Injectable({ providedIn: 'root' })
export class AdminEmails {
  private readonly baseUrl = '/api/admin/emails';

  constructor(private readonly http: HttpClient) {}

  list(query: EmailQuery): Observable<EmailPage> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.category) {
      params = params.set('category', query.category);
    }
    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }
    return this.http.get<EmailPage>(this.baseUrl, { params });
  }

  stats(): Observable<EmailStats> {
    return this.http.get<EmailStats>(`${this.baseUrl}/stats`);
  }

  get(emailId: string): Observable<EmailDetail> {
    return this.http.get<EmailDetail>(`${this.baseUrl}/${emailId}`);
  }

  attachmentUrl(emailId: string, attachmentId: string): string {
    return `${this.baseUrl}/${emailId}/attachments/${attachmentId}`;
  }

  process(batchSize = 50): Observable<EmailProcessResult> {
    return this.http.post<EmailProcessResult>(`${this.baseUrl}/process`, { batchSize });
  }

  retry(emailId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${emailId}/retry`, {});
  }

  cancel(emailId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${emailId}/cancel`, {});
  }

  compose(letter: ComposeLetter): Observable<{ emailId: string }> {
    const form = new FormData();
    form.append('toEmail', letter.toEmail);
    if (letter.toName) {
      form.append('toName', letter.toName);
    }
    form.append('subject', letter.subject);
    form.append('message', letter.message);
    for (const file of letter.files) {
      form.append('files', file, file.name);
    }
    return this.http.post<{ emailId: string }>(this.baseUrl, form);
  }

  emailInvoice(invoiceId: string): Observable<{ emailId: string }> {
    return this.http.post<{ emailId: string }>(`${this.baseUrl}/invoices/${invoiceId}`, {});
  }
}
