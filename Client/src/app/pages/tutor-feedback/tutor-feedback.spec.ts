import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TutorFeedback } from './tutor-feedback';

describe('TutorFeedback', () => {
  let component: TutorFeedback;
  let fixture: ComponentFixture<TutorFeedback>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorFeedback],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorFeedback);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/feedback').flush({
      averageRating: 4.5, reviewCount: 2, distribution: [0, 0, 0, 1, 1],
      reviews: [
        { reviewId: '1', studentName: 'A', subjectName: 'M', rating: 5, comment: 'Great', createdAt: '2026-10-01', lessonDate: '2026-10-01' },
        { reviewId: '2', studentName: 'B', subjectName: 'M', rating: 4, comment: null, createdAt: '2026-09-01', lessonDate: '2026-09-01' },
      ],
    });
  });

  afterEach(() => http.verify());

  it('filters by star rating and comments', () => {
    component.setStars(4);
    expect(component.filtered().map((r) => r.reviewId)).toEqual(['2']);
    component.setStars(4);
    component.toggleComments();
    expect(component.filtered().map((r) => r.reviewId)).toEqual(['1']);
  });

  it('draws half stars for the average', () => {
    expect(component.starIcons(4.5)).toEqual(['bi-star-fill', 'bi-star-fill', 'bi-star-fill', 'bi-star-fill', 'bi-star-half']);
  });
});
