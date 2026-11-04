import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ChatMessage, Contact, Conversation, MAX_MESSAGE_LENGTH } from '../../models/messages.model';
import { UserRole, roleLabel } from '../../models/user-role.model';
import { Messages as MessagesApi } from '../../services/messages';
import { NotificationService } from '../../services/notification.service';
import { PageHeader } from '../../components/page-header/page-header';
import { Avatar } from '../../components/avatar/avatar';
import { Dialog } from '../../components/dialog/dialog';

interface ThreadItem extends ChatMessage {
  pending?: boolean;
  failed?: boolean;
}

interface DayGroup {
  label: string;
  items: ThreadItem[];
}

const THREAD_POLL_MS = 5_000;
const LIST_POLL_MS = 20_000;

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeader, Avatar, Dialog],
  templateUrl: './messages.html',
  styleUrl: './messages.scss',
})
export class MessagesPage implements OnInit, OnDestroy {
  @ViewChild('scroller') scroller?: ElementRef<HTMLElement>;
  @ViewChild('composer') composer?: ElementRef<HTMLTextAreaElement>;

  readonly loadingList = signal(true);
  readonly listFailed = signal(false);
  readonly conversations = signal<Conversation[]>([]);
  readonly selectedId = signal<string | null>(null);
  readonly loadingThread = signal(false);
  readonly threadFailed = signal(false);
  readonly thread = signal<ThreadItem[]>([]);
  readonly hasMore = signal(false);
  readonly loadingOlder = signal(false);
  readonly draft = signal('');
  readonly sending = signal(false);
  readonly search = signal('');

  readonly composeOpen = signal(false);
  readonly contacts = signal<Contact[]>([]);
  readonly contactsLoading = signal(false);
  readonly composeTo = signal<{ userId: string; name: string } | null>(null);
  readonly composeBody = signal('');
  readonly composeError = signal<string | null>(null);
  readonly composeSending = signal(false);

  readonly maxLength = MAX_MESSAGE_LENGTH;
  readonly roleName = roleLabel;

  readonly selected = computed(() => this.conversations().find((c) => c.conversationId === this.selectedId()) ?? null);
  readonly filteredConversations = computed(() => {
    const q = this.search().trim().toLowerCase();
    return q ? this.conversations().filter((c) => c.otherName.toLowerCase().includes(q)) : this.conversations();
  });
  readonly groups = computed<DayGroup[]>(() => {
    const groups: DayGroup[] = [];
    for (const m of this.thread()) {
      const label = this.dayLabel(m.sentAt);
      const last = groups[groups.length - 1];
      if (last && last.label === label) {
        last.items.push(m);
      } else {
        groups.push({ label, items: [m] });
      }
    }
    return groups;
  });
  readonly lastMineRead = computed(() => {
    const mine = this.thread().filter((m) => m.mine && !m.pending);
    const last = mine[mine.length - 1];
    return last?.readAt ? last.messageId : null;
  });

  role: UserRole = UserRole.Student;
  private threadTimer: ReturnType<typeof setInterval> | null = null;
  private listTimer: ReturnType<typeof setInterval> | null = null;
  private threadSub: Subscription | null = null;

  constructor(
    private readonly api: MessagesApi,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    let current: ActivatedRoute | null = this.route;
    while (current && !current.snapshot.data['role']) {
      current = current.parent;
    }
    this.role = (current?.snapshot.data['role'] as UserRole) ?? UserRole.Student;

    this.loadConversations(false, () => this.applyQuery());
    this.listTimer = setInterval(() => this.visible() && this.loadConversations(true), LIST_POLL_MS);
    this.threadTimer = setInterval(() => this.visible() && this.selectedId() && this.refreshThread(), THREAD_POLL_MS);
  }

  ngOnDestroy(): void {
    if (this.threadTimer) {
      clearInterval(this.threadTimer);
    }
    if (this.listTimer) {
      clearInterval(this.listTimer);
    }
    this.threadSub?.unsubscribe();
  }

  loadConversations(silent = false, then?: () => void): void {
    if (!silent) {
      this.listFailed.set(false);
    }
    this.api.getConversations(silent).subscribe({
      next: (list) => {
        this.conversations.set(list);
        this.loadingList.set(false);
        then?.();
      },
      error: () => {
        this.loadingList.set(false);
        if (!silent) {
          this.listFailed.set(true);
        }
      },
    });
  }

  open(conversation: Conversation): void {
    this.router.navigate([], { queryParams: { c: conversation.conversationId }, replaceUrl: !!this.selectedId() });
    this.select(conversation.conversationId);
  }

  back(): void {
    this.selectedId.set(null);
    this.thread.set([]);
    this.router.navigate([], { queryParams: {}, replaceUrl: true });
  }

  select(id: string): void {
    if (this.selectedId() === id && this.thread().length) {
      return;
    }
    this.selectedId.set(id);
    this.thread.set([]);
    this.hasMore.set(false);
    this.threadFailed.set(false);
    this.loadingThread.set(true);
    this.draft.set('');
    this.threadSub?.unsubscribe();
    this.threadSub = this.api.getMessages(id).subscribe({
      next: (page) => {
        this.thread.set(page.messages);
        this.hasMore.set(page.hasMore);
        this.loadingThread.set(false);
        this.afterIncoming(page.conversation.unreadCount, true);
        setTimeout(() => this.composer?.nativeElement.focus(), 0);
      },
      error: () => {
        this.loadingThread.set(false);
        this.threadFailed.set(true);
      },
    });
  }

  loadOlder(): void {
    const id = this.selectedId();
    const first = this.thread().find((m) => !m.pending);
    if (!id || !first) {
      return;
    }
    const el = this.scroller?.nativeElement;
    const fromBottom = el ? el.scrollHeight - el.scrollTop : 0;
    this.loadingOlder.set(true);
    this.api.getMessages(id, first.sentAt).subscribe({
      next: (page) => {
        this.thread.update((t) => [...page.messages, ...t]);
        this.hasMore.set(page.hasMore);
        this.loadingOlder.set(false);
        setTimeout(() => el && (el.scrollTop = el.scrollHeight - fromBottom), 0);
      },
      error: () => this.loadingOlder.set(false),
    });
  }

  onComposerKey(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.send();
    }
  }

  send(retry?: ThreadItem): void {
    const id = this.selectedId();
    const body = (retry?.body ?? this.draft()).trim();
    if (!id || !body || body.length > this.maxLength) {
      return;
    }
    const temp: ThreadItem = retry
      ? { ...retry, failed: false, pending: true }
      : { messageId: 'tmp-' + Date.now(), body, sentAt: new Date().toISOString(), mine: true, readAt: null, pending: true };

    if (retry) {
      this.thread.update((t) => t.map((m) => (m.messageId === retry.messageId ? temp : m)));
    } else {
      this.thread.update((t) => [...t, temp]);
      this.draft.set('');
    }
    this.scrollToBottom();

    this.api.send(id, body).subscribe({
      next: (saved) => {
        this.thread.update((t) => t.map((m) => (m.messageId === temp.messageId ? saved : m)));
        this.conversations.update((list) =>
          [
            ...list.filter((c) => c.conversationId === id).map((c) => ({ ...c, lastMessage: body, lastMessageAt: saved.sentAt, lastMessageMine: true })),
            ...list.filter((c) => c.conversationId !== id),
          ]
        );
      },
      error: (err: HttpErrorResponse) => {
        this.thread.update((t) => t.map((m) => (m.messageId === temp.messageId ? { ...m, pending: false, failed: true } : m)));
        if (err.status === 429) {
          this.notifications.showError('Slow down a little', 'You’re sending messages too quickly. Wait a moment and try again.');
        } else if (err.error?.message) {
          this.notifications.showError('Message not sent', err.error.message);
        }
      },
    });
  }

  discard(item: ThreadItem): void {
    this.thread.update((t) => t.filter((m) => m.messageId !== item.messageId));
  }

  openCompose(to?: { userId: string; name: string }): void {
    this.composeError.set(null);
    this.composeBody.set('');
    this.composeTo.set(to ?? null);
    this.composeOpen.set(true);
    if (!to && !this.contacts().length) {
      this.contactsLoading.set(true);
      this.api.getContacts().subscribe({
        next: (c) => {
          this.contacts.set(c);
          this.contactsLoading.set(false);
        },
        error: () => this.contactsLoading.set(false),
      });
    }
  }

  pickContact(contact: Contact): void {
    const existing = this.conversations().find((c) => c.otherUserId === contact.userId);
    if (existing) {
      this.composeOpen.set(false);
      this.open(existing);
      return;
    }
    this.composeTo.set({ userId: contact.userId, name: contact.fullName });
  }

  sendNew(): void {
    const to = this.composeTo();
    const body = this.composeBody().trim();
    this.composeError.set(null);
    if (!to) {
      this.composeError.set('Choose who to message.');
      return;
    }
    if (!body) {
      this.composeError.set('Write a message first.');
      return;
    }
    this.composeSending.set(true);
    this.api.start(to.userId, body).subscribe({
      next: (conversation) => {
        this.composeSending.set(false);
        this.composeOpen.set(false);
        this.conversations.update((list) => [conversation, ...list.filter((c) => c.conversationId !== conversation.conversationId)]);
        this.open(conversation);
      },
      error: (err: HttpErrorResponse) => {
        this.composeSending.set(false);
        this.composeError.set(err.error?.message ?? 'Your message wasn’t sent. Try again.');
      },
    });
  }

  closeCompose(): void {
    this.composeOpen.set(false);
    if (this.route.snapshot.queryParamMap.has('to')) {
      this.router.navigate([], { queryParams: {}, replaceUrl: true });
    }
  }

  timeLabel(iso: string | null): string {
    if (!iso) {
      return '';
    }
    const date = new Date(iso);
    const now = new Date();
    if (date.toDateString() === now.toDateString()) {
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
    }
    const days = Math.floor((now.getTime() - date.getTime()) / 86_400_000);
    if (days < 7) {
      return date.toLocaleDateString([], { weekday: 'short' });
    }
    return date.toLocaleDateString([], { day: 'numeric', month: 'short' });
  }

  private dayLabel(iso: string): string {
    const date = new Date(iso);
    const today = new Date();
    const yesterday = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1);
    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    }
    if (date.toDateString() === yesterday.toDateString()) {
      return 'Yesterday';
    }
    return date.toLocaleDateString([], { weekday: 'long', day: 'numeric', month: 'long' });
  }

  private applyQuery(): void {
    const q = this.route.snapshot.queryParamMap;
    const c = q.get('c');
    const to = q.get('to');
    if (c && this.conversations().some((x) => x.conversationId === c)) {
      this.select(c);
    } else if (to) {
      const existing = this.conversations().find((x) => x.otherUserId === to);
      if (existing) {
        this.open(existing);
      } else {
        this.openCompose({ userId: to, name: q.get('name') ?? 'this person' });
      }
    }
  }

  private refreshThread(): void {
    const id = this.selectedId();
    if (!id || this.loadingThread()) {
      return;
    }
    this.api.getMessages(id, undefined, true).subscribe({
      next: (page) => {
        if (this.selectedId() !== id) {
          return;
        }
        const pending = this.thread().filter((m) => m.pending || m.failed);
        const oldest = page.messages[0]?.sentAt;
        const older = oldest ? this.thread().filter((m) => !m.pending && !m.failed && m.sentAt < oldest) : [];
        const before = this.thread().filter((m) => !m.pending && !m.failed).length;
        this.thread.set([...older, ...page.messages, ...pending]);
        const grew = this.thread().filter((m) => !m.pending && !m.failed).length > before;
        this.afterIncoming(page.conversation.unreadCount, grew);
      },
    });
  }

  private afterIncoming(unread: number, scroll: boolean): void {
    const id = this.selectedId();
    if (id && unread > 0) {
      this.api.markRead(id).subscribe();
      this.conversations.update((list) => list.map((c) => (c.conversationId === id ? { ...c, unreadCount: 0 } : c)));
    }
    if (scroll) {
      this.scrollToBottom();
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.scroller?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    }, 0);
  }

  private visible(): boolean {
    return document.visibilityState === 'visible';
  }
}
