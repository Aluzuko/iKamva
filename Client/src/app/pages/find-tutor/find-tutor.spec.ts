import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { FindTutor } from './find-tutor';

describe('FindTutor', () => {
  let component: FindTutor;
  let fixture: ComponentFixture<FindTutor>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FindTutor],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(FindTutor);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/student/subjects').flush([{ subjectId: 'm', name: 'Mathematics' }]);
    http.expectOne((r) => r.url === '/api/student/tutors').flush([]);
  });

  afterEach(() => http.verify());

  it('sends filters to the API and drops empty ones', () => {
    component.update({ subjectId: 'm', maxRate: 75 });
    const req = http.expectOne((r) => r.url === '/api/student/tutors');
    expect(req.request.params.get('subjectId')).toBe('m');
    expect(req.request.params.get('maxRate')).toBe('75');
    expect(req.request.params.has('minRating')).toBe(false);
    req.flush([]);
    expect(component.hasFilters).toBe(true);
  });

  it('clears filters', () => {
    component.update({ minRating: 4 });
    http.expectOne((r) => r.url === '/api/student/tutors').flush([]);
    component.clear();
    const req = http.expectOne((r) => r.url === '/api/student/tutors');
    expect(req.request.params.keys()).toEqual(['sort']);
    req.flush([]);
  });
});
