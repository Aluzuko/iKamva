import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Tutor } from './tutor';

describe('Tutor', () => {
  let service: Tutor;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(Tutor);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uploads documents as multipart form data', () => {
    const file = new File(['%PDF-1.4'], 'id.pdf', { type: 'application/pdf' });
    service.uploadDocument('idDocument', file).subscribe();
    const req = http.expectOne('/api/tutor/documents');
    expect(req.request.body instanceof FormData).toBe(true);
    expect((req.request.body as FormData).get('documentType')).toBe('idDocument');
    req.flush({});
  });

  it('sends the whole week when saving availability', () => {
    service.updateAvailability([{ dayOfWeek: 1, startMinute: 900, endMinute: 1080 }]).subscribe();
    const req = http.expectOne('/api/tutor/availability');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ slots: [{ dayOfWeek: 1, startMinute: 900, endMinute: 1080 }] });
    req.flush(null);
  });
});
