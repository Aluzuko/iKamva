import { Injectable, OnDestroy, signal } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable, Subscription, catchError, of, tap } from 'rxjs';
import { ChatMessage, Contact, Conversation, MessagePage } from '../models/messages.model';
import { SILENT_REQUEST } from './loading.service';

const UNREAD_POLL_MS = 30_000;

@Injectable({
  providedIn: 'root',
})
export class Messages implements OnDestroy {
  private readonly baseUrl = '/api/messages';
  private timer: ReturnType<typeof setInterval> | null = null;
  private pending: Subscription | null = null;
  private readonly onVisible = () => {
    if (document.visibilityState === 'visible') {
      this.refreshUnread();
    }
  };

  readonly unread = signal(0);

  constructor(private readonly http: HttpClient) {}

  silent(): { context: HttpContext } {
    return { context: new HttpContext().set(SILENT_REQUEST, true) };
  }

  startUnreadPolling(): void {
    if (this.timer) {
      return;
    }
    this.refreshUnread();
    this.timer = setInterval(() => {
      if (document.visibilityState === 'visible') {
        this.refreshUnread();
      }
    }, UNREAD_POLL_MS);
    document.addEventListener('visibilitychange', this.onVisible);
  }

  stopUnreadPolling(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    document.removeEventListener('visibilitychange', this.onVisible);
    this.pending?.unsubscribe();
    this.unread.set(0);
  }

  refreshUnread(): void {
    this.pending?.unsubscribe();
    this.pending = this.http
      .get<{ unread: number }>(`${this.baseUrl}/unread-count`, this.silent())
      .pipe(catchError(() => of(null)))
      .subscribe((r) => {
        if (r) {
          this.unread.set(r.unread);
        }
      });
  }

  getConversations(silent = false): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.baseUrl}/conversations`, silent ? this.silent() : {});
  }

  getMessages(conversationId: string, before?: string, silent = false): Observable<MessagePage> {
    const params = before ? new HttpParams().set('before', before) : undefined;
    return this.http.get<MessagePage>(`${this.baseUrl}/conversations/${conversationId}`, { params, ...(silent ? this.silent() : {}) });
  }

  send(conversationId: string, body: string): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(`${this.baseUrl}/conversations/${conversationId}/messages`, { body }, this.silent());
  }

  start(recipientUserId: string, body: string): Observable<Conversation> {
    return this.http.post<Conversation>(`${this.baseUrl}/conversations`, { recipientUserId, body });
  }

  markRead(conversationId: string): Observable<void> {
    return this.http
      .post<void>(`${this.baseUrl}/conversations/${conversationId}/read`, {}, this.silent())
      .pipe(tap(() => this.refreshUnread()));
  }

  getContacts(): Observable<Contact[]> {
    return this.http.get<Contact[]>(`${this.baseUrl}/contacts`);
  }

  ngOnDestroy(): void {
    this.stopUnreadPolling();
  }
}
