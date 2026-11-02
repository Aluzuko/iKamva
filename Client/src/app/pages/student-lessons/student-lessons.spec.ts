import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { StudentLessons } from './student-lessons';

const lesson = (id: string, status: string, hours: number, over: object = {}) => ({
  bookingId: id, tutorId: 't', tutorName: 'Dr. Sarah Chen', subjectName: 'Mathematics', scheduledAt: new Date(Date.now() + hours * 3600000).toISOString(),
  durationMinutes: 60, price: 85, status, notes: null, recordingUrl: null, cancellationReason: null,
  canCancel: status === 'upcoming', canReview: status === 'completed', myRating: null, myComment: null, ...over,
});

describe('StudentLessons', () => {
  let component: StudentLessons;
  let fixture: ComponentFixture<StudentLessons>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentLessons],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(StudentLessons);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/student/lessons').flush([lesson('a', 'upcoming', 30), lesson('b', 'completed', -48)]);
  });

  afterEach(() => http.verify());

  it('requires a star rating before sending a review', () => {
    component.openReview(component.past()[0]);
    component.submitReview();
    http.expectNone('/api/student/lessons/b/review');
    expect(component.dialogError()).toBe('Choose a star rating.');
  });

  it('saves the rating and hides the Rate button', () => {
    component.openReview(component.past()[0]);
    component.rating.set(4);
    component.reviewForm.setValue({ comment: '  Clear explanations  ' });
    component.submitReview();
    const req = http.expectOne('/api/student/lessons/b/review');
    expect(req.request.body).toEqual({ rating: 4, comment: 'Clear explanations' });
    req.flush(null);
    expect(component.toReview()).toBe(0);
    expect(component.past()[0].myRating).toBe(4);
  });

  it('cancels an upcoming lesson with a reason', () => {
    component.openCancel(component.upcoming()[0]);
    component.cancelForm.setValue({ reason: 'School trip that day' });
    component.confirmCancel();
    http.expectOne('/api/student/lessons/a/cancel').flush(null);
    expect(component.upcoming().length).toBe(0);
  });
});
