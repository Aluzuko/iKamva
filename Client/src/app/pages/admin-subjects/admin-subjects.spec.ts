import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminSubjects } from './admin-subjects';

describe('AdminSubjects', () => {
  let component: AdminSubjects;
  let fixture: ComponentFixture<AdminSubjects>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSubjects],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminSubjects);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/subjects').flush([{ subjectId: 's1', name: 'Maths', defaultHourlyRate: 40, commissionPercent: 15, tutorCount: 3 }]);
  });

  afterEach(() => http.verify());

  it('works out what the tutor receives', () => {
    expect(component.tutorReceives(40, 15)).toBe(34);
  });

  it('rejects a commission above 100%', () => {
    component.openEdit(component.subjects()[0]);
    component.form.patchValue({ commissionPercent: 120 });
    component.submit();
    http.expectNone('/api/admin/subjects/s1/pricing');
  });

  it('saves pricing and updates the row', () => {
    component.openEdit(component.subjects()[0]);
    component.form.patchValue({ defaultHourlyRate: 50, commissionPercent: 20 });
    component.submit();
    const req = http.expectOne('/api/admin/subjects/s1/pricing');
    expect(req.request.body).toEqual({ defaultHourlyRate: 50, commissionPercent: 20 });
    req.flush(null);
    expect(component.subjects()[0].defaultHourlyRate).toBe(50);
    expect(component.editing()).toBeNull();
  });
});
