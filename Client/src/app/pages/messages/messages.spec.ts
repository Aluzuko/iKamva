import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { MessagesPage } from './messages';

const conv = { conversationId: 'c1', otherUserId: 't1', otherName: 'Dr. Sarah Chen', otherRole: 'tutor', lastMessage: 'Hi', lastMessageAt: '2026-10-22T10:00:00Z', lastMessageMine: false, unreadCount: 1 };

describe('MessagesPage', () => {
  let component: MessagesPage;
  let fixture: ComponentFixture<MessagesPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessagesPage],
      providers: [
        provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ c: 'c1' }), data: { role: 'student' } }, parent: null } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MessagesPage);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/messages/conversations').flush([conv]);
    http.expectOne('/api/messages/conversations/c1').flush({
      conversation: conv, hasMore: false,
      messages: [{ messageId: 'm1', body: 'Hi', sentAt: '2026-10-22T10:00:00Z', mine: false, readAt: null }],
    });
  });

  afterEach(() => {
    component.ngOnDestroy();
    http.verify();
  });

  it('opens the conversation from the link and marks it read', () => {
    http.expectOne('/api/messages/conversations/c1/read').flush(null);
    http.expectOne('/api/messages/unread-count').flush({ unread: 0 });
    expect(component.thread().length).toBe(1);
    expect(component.conversations()[0].unreadCount).toBe(0);
  });

  it('shows a sent message straight away and lets you retry a failure', () => {
    http.expectOne('/api/messages/conversations/c1/read').flush(null);
    http.expectOne('/api/messages/unread-count').flush({ unread: 0 });
    component.draft.set('See you Friday');
    component.send();
    expect(component.thread()[1].pending).toBe(true);
    expect(component.draft()).toBe('');
    http.expectOne('/api/messages/conversations/c1/messages').flush({}, { status: 500, statusText: 'Error' });
    const failed = component.thread()[1];
    expect(failed.failed).toBe(true);
    component.send(failed);
    http.expectOne('/api/messages/conversations/c1/messages').flush({ messageId: 'm2', body: 'See you Friday', sentAt: '2026-10-22T10:05:00Z', mine: true, readAt: null });
    expect(component.thread().map((m) => m.messageId)).toEqual(['m1', 'm2']);
  });
});
