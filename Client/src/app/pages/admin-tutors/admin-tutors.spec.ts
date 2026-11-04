import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminTutors } from './admin-tutors';

const tutor = (id: string, status: 'pending' | 'approved' | 'rejected', name = 'Tutor ' + id) => ({
  tutorId: id, fullName: name, email: id + '@x.com', hourlyRate: 30, yearsExperience: 2,
  verificationStatus: status, isActive: true, subjects: ['Maths'], completedBookings: 0,
});

describe('AdminTutors', () => {
  let component: AdminTutors;
  let fixture: ComponentFixture<AdminTutors>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminTutors],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminTutors);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/tutors/verification-queue').flush([]);
    http.expectOne('/api/admin/tutors').flush([tutor('a', 'approved', 'Ayanda'), tutor('b', 'pending', 'Bongi'), tutor('c', 'approved', 'Cara')]);
  });

  afterEach(() => http.verify());

  it('filters by verification status and search', () => {
    expect(component.countFor('approved')).toBe(2);
    component.setFilter('approved');
    expect(component.filtered().length).toBe(2);
    component.onSearch('cara');
    expect(component.filtered().map((t) => t.fullName)).toEqual(['Cara']);
  });

  it('requires a reason before rejecting', () => {
    component.openReject({ tutorId: 'b', fullName: 'Bongi', verificationStatus: 'pending' });
    component.confirmReject();
    http.expectNone('/api/admin/tutors/b/reject');
    expect(component.rejectInvalid).toBe(true);
  });

  it('shows a duplicate email message on 409', () => {
    component.openAdd();
    http.expectOne('/api/admin/subjects').flush([]);
    component.addForm.setValue({ fullName: 'New Tutor', email: 'new@x.com', password: 'Passw0rd1', hourlyRate: 40, yearsExperience: 1, bio: '', subjectIds: [] });
    component.submitAdd();
    http.expectOne('/api/admin/tutors').flush({}, { status: 409, statusText: 'Conflict' });
    expect(component.addError()).toBe('An account with this email already exists.');
  });
});
