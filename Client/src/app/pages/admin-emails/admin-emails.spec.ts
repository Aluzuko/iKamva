import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminEmails } from './admin-emails';

const stats = { pending: 2, due: 2, sending: 0, sent: 5, failed: 1, cancelled: 0, oldestDueAt: null, workerEnabled: false, transport: 'Pickup' };
const item = (id: string, status: string) => ({
  emailId: id, category: 'invoice', status, priority: 'normal', toEmail: 'maria@example.com', toName: 'Maria Thompson', subject: 'Invoice',
  attempts: 0, maxAttempts: 5, attachmentCount: 1, createdAt: '2026-10-22T10:00:00Z', nextAttemptAt: '2026-10-22T10:00:00Z', sentAt: null, lastError: null,
});

describe('AdminEmails', () => {
  let component: AdminEmails;
  let fixture: ComponentFixture<AdminEmails>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminEmails],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminEmails);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/emails/stats').flush(stats);
    http.expectOne((r) => r.url === '/api/admin/emails').flush({ items: [item('e1', 'pending')], total: 1, page: 1, pageSize: 20 });
  });

  afterEach(() => http.verify());

  it('filters by status on the server', () => {
    component.setFilter('failed');
    const req = http.expectOne((r) => r.url === '/api/admin/emails');
    expect(req.request.params.get('status')).toBe('failed');
    req.flush({ items: [], total: 0, page: 1, pageSize: 20 });
  });

  it('processes the queue and refreshes', () => {
    component.process();
    const req = http.expectOne('/api/admin/emails/process');
    expect(req.request.body).toEqual({ batchSize: 50 });
    req.flush({ claimed: 2, sent: 2, retrying: 0, failed: 0 });
    http.expectOne('/api/admin/emails/stats').flush({ ...stats, due: 0, pending: 0, sent: 7 });
    http.expectOne((r) => r.url === '/api/admin/emails').flush({ items: [], total: 0, page: 1, pageSize: 20 });
    expect(component.stats()?.sent).toBe(7);
  });

  it('blocks dangerous attachments and oversized totals', () => {
    const pick = (files: File[]) => component.addFiles({ target: { files, value: 'x' } } as unknown as Event);
    pick([new File(['x'], 'setup.exe')]);
    expect(component.fileError()).toContain('setup.exe');
    pick([new File([new Uint8Array(11 * 1024 * 1024)], 'big.pdf')]);
    expect(component.fileError()).toContain('10 MB');
    pick([new File(['%PDF'], 'letter.pdf')]);
    expect(component.files().length).toBe(1);
  });

  it('sends the letter as multipart with files', () => {
    component.openCompose();
    component.files.set([new File(['%PDF'], 'letter.pdf', { type: 'application/pdf' })]);
    component.composeForm.setValue({ toEmail: 'parent@example.com', toName: '', subject: 'Term dates', message: 'Hello there' });
    component.sendLetter();
    const req = http.expectOne('/api/admin/emails');
    const body = req.request.body as FormData;
    expect(body.get('toEmail')).toBe('parent@example.com');
    expect((body.get('files') as File).name).toBe('letter.pdf');
    req.flush({ emailId: 'e2' });
    http.expectOne('/api/admin/emails/stats').flush(stats);
    http.expectOne((r) => r.url === '/api/admin/emails').flush({ items: [], total: 0, page: 1, pageSize: 20 });
    expect(component.composeOpen()).toBe(false);
  });
});
