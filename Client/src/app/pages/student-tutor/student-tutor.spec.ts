import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { StudentTutor } from './student-tutor';

const tutor = {
  tutorId: 't1', fullName: 'Dr. Sarah Chen', bio: 'Maths', hourlyRate: 80, yearsExperience: 12,
  subjects: [{ subjectId: 'm', name: 'Mathematics' }], averageRating: 4.8, reviewCount: 3, completedLessons: 40,
  availability: [{ dayOfWeek: 1, startMinute: 900, endMinute: 1080 }], reviews: [],
};
const slots = { timeZone: 'Africa/Johannesburg', durationMinutes: 60, days: [
  { date: '2026-10-26', starts: [] },
  { date: '2026-10-27', starts: ['2026-10-27T13:00:00Z', '2026-10-27T13:30:00Z'] },
] };

describe('StudentTutor', () => {
  let component: StudentTutor;
  let fixture: ComponentFixture<StudentTutor>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentTutor],
      providers: [
        provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ tutorId: 't1' }), queryParamMap: convertToParamMap({}), data: {} }, parent: null } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(StudentTutor);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/student/tutors/t1').flush(tutor);
    http.expectOne((r) => r.url === '/api/student/tutors/t1/slots').flush(slots);
  });

  afterEach(() => http.verify());

  it('preselects the only subject and the first day with free times', () => {
    expect(component.subjectId()).toBe('m');
    expect(component.selectedDate()).toBe('2026-10-27');
  });

  it('prices by length and reloads times when the length changes', () => {
    expect(component.priceFor(90)).toBe(120);
    component.setDuration(90);
    const req = http.expectOne((r) => r.url === '/api/student/tutors/t1/slots');
    expect(req.request.params.get('duration')).toBe('90');
    req.flush({ ...slots, durationMinutes: 90 });
    expect(component.price()).toBe(120);
  });

  it('reloads times when the slot was just taken', () => {
    component.selectedStart.set('2026-10-27T13:00:00Z');
    component.confirm();
    const book = http.expectOne('/api/student/bookings');
    expect(book.request.body).toEqual({ tutorId: 't1', subjectId: 'm', startUtc: '2026-10-27T13:00:00Z', durationMinutes: 60 });
    book.flush({ message: 'That time was just taken. Pick another slot.' }, { status: 409, statusText: 'Conflict' });
    expect(component.bookError()).toContain('just taken');
    http.expectOne((r) => r.url === '/api/student/tutors/t1/slots').flush(slots);
    expect(component.selectedStart()).toBeNull();
  });
});
