import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TutorLessons } from './tutor-lessons';

const lesson = (id: string, status: string, hours: number, needsCompletion = false) => ({
  bookingId: id, studentName: 'Alex Thompson', subjectName: 'Mathematics', scheduledAt: new Date(Date.now() + hours * 3600000).toISOString(),
  durationMinutes: 60, price: 85, earning: 72.25, status, notes: null, recordingUrl: null, cancellationReason: null, needsCompletion, rating: null,
});

describe('TutorLessons', () => {
  let component: TutorLessons;
  let fixture: ComponentFixture<TutorLessons>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorLessons],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorLessons);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/lessons').flush([lesson('a', 'upcoming', 48), lesson('b', 'upcoming', -5, true), lesson('c', 'completed', -72)]);
  });

  afterEach(() => http.verify());

  it('separates lessons to confirm from upcoming ones', () => {
    expect(component.toConfirm().map((l) => l.bookingId)).toEqual(['b']);
    expect(component.upcoming().map((l) => l.bookingId)).toEqual(['a']);
  });

  it('marks a lesson as completed', () => {
    component.complete(component.toConfirm()[0]);
    http.expectOne('/api/tutor/lessons/b/complete').flush(null);
    expect(component.toConfirm().length).toBe(0);
    expect(component.pastCount('completed')).toBe(2);
  });

  it('needs a reason to cancel', () => {
    component.openCancel(component.upcoming()[0]);
    component.confirmCancel();
    http.expectNone('/api/tutor/lessons/a/cancel');
    component.cancelForm.setValue({ reason: 'I am unwell today' });
    component.confirmCancel();
    http.expectOne('/api/tutor/lessons/a/cancel').flush(null);
    expect(component.upcoming().length).toBe(0);
  });
});
