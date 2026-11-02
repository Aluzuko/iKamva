import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TutorNotes } from './tutor-notes';

const lesson = (id: string, notes: string | null) => ({
  bookingId: id, studentName: 'Alex Thompson', subjectName: 'Mathematics', scheduledAt: '2026-10-01T14:00:00Z',
  durationMinutes: 60, price: 85, earning: 72.25, status: 'completed', notes, recordingUrl: null, cancellationReason: null, needsCompletion: false, rating: null,
});

describe('TutorNotes', () => {
  let component: TutorNotes;
  let fixture: ComponentFixture<TutorNotes>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorNotes],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ lesson: 'a' }), data: {} }, parent: null } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorNotes);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/lessons').flush([lesson('a', null), lesson('b', 'Covered fractions')]);
  });

  afterEach(() => http.verify());

  it('opens the lesson named in the link', () => {
    expect(component.editing()?.bookingId).toBe('a');
    expect(component.filtered().map((l) => l.bookingId)).toEqual(['a']);
  });

  it('rejects blank notes and bad recording links', () => {
    component.form.setValue({ notes: '   ', recordingUrl: 'not a link' });
    component.save();
    http.expectNone('/api/tutor/lessons/a/notes');
    expect(component.invalid('recordingUrl')).toBe(true);
  });

  it('saves notes and updates the list', () => {
    component.form.setValue({ notes: 'Integration by parts', recordingUrl: 'https://example.com/rec' });
    component.save();
    const req = http.expectOne('/api/tutor/lessons/a/notes');
    expect(req.request.body).toEqual({ notes: 'Integration by parts', recordingUrl: 'https://example.com/rec' });
    req.flush(null);
    expect(component.countFor('missing')).toBe(0);
    expect(component.editing()).toBeNull();
  });
});
