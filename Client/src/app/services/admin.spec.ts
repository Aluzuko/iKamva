import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Admin } from './admin';
import { UserRole } from '../models/user-role.model';

describe('Admin', () => {
  let service: Admin;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(Admin);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the rejection reason in the body', () => {
    service.rejectTutor('t1', 'Expired certificate').subscribe();
    const req = http.expectOne('/api/admin/tutors/t1/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'Expired certificate' });
    req.flush(null);
  });

  it('filters users by role with a query parameter', () => {
    service.getUsers(UserRole.Parent).subscribe();
    http.expectOne('/api/admin/users?role=parent').flush([]);
  });

  it('updates subject pricing with PUT', () => {
    service.updateSubjectPricing('s1', { defaultHourlyRate: 30, commissionPercent: 15 }).subscribe();
    const req = http.expectOne('/api/admin/subjects/s1/pricing');
    expect(req.request.method).toBe('PUT');
    req.flush(null);
  });

  it('posts dispute outcomes to resolve and reject', () => {
    service.resolveDispute('d1', 'Refunded').subscribe();
    service.rejectDispute('d2', 'Lesson happened').subscribe();
    const resolve = http.expectOne('/api/admin/disputes/d1/resolve');
    const reject = http.expectOne('/api/admin/disputes/d2/reject');
    expect(resolve.request.body).toEqual({ resolution: 'Refunded' });
    expect(reject.request.body).toEqual({ resolution: 'Lesson happened' });
    resolve.flush(null);
    reject.flush(null);
  });
});
