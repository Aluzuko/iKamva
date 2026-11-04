import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminAnalytics } from './admin-analytics';

describe('AdminAnalytics', () => {
  let component: AdminAnalytics;
  let fixture: ComponentFixture<AdminAnalytics>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminAnalytics],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminAnalytics);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('combines the monthly series into rows and totals', () => {
    http.expectOne('/api/admin/analytics').flush({
      revenueByMonth: [{ label: 'Sep', value: 300 }, { label: 'Oct', value: 100 }],
      bookingsByMonth: [{ label: 'Sep', value: 9 }, { label: 'Oct', value: 3 }],
      cancellationsByMonth: [{ label: 'Sep', value: 1 }, { label: 'Oct', value: 1 }],
    });
    expect(component.rows()[0]).toEqual({ label: 'Sep', revenue: 300, bookings: 9, cancellations: 1, cancelRate: 10 });
    expect(component.totals().bookings).toBe(12);
    expect(component.totals().average).toBeCloseTo(33.33, 2);
  });

  it('shows the empty state when there is no activity', () => {
    http.expectOne('/api/admin/analytics').flush({ revenueByMonth: [{ label: 'Oct', value: 0 }], bookingsByMonth: [{ label: 'Oct', value: 0 }], cancellationsByMonth: [{ label: 'Oct', value: 0 }] });
    expect(component.hasActivity()).toBe(false);
  });
});
