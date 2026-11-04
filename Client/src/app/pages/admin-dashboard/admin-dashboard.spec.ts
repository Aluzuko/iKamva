import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminDashboard } from './admin-dashboard';

describe('AdminDashboard', () => {
  let component: AdminDashboard;
  let fixture: ComponentFixture<AdminDashboard>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboard],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboard);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('loads the admin dashboard endpoint', () => {
    http.expectOne('/api/admin/dashboard').flush({
      totalRevenue: 100,
      revenueChangePercent: 0,
      totalBookings: 2,
      bookingsChangePercent: 0,
      activeTutors: 1,
      activeStudents: 1,
      upcomingBookings: 1,
      completedBookings: 1,
      cancelledBookings: 0,
      recentBookings: [],
    });
    expect(component.dashboard()?.totalBookings).toBe(2);
  });

  it('treats an empty response as no data instead of crashing', () => {
    http.expectOne('/api/admin/dashboard').flush(null);
    expect(component.dashboard()).toBeNull();
    expect(component.loading()).toBe(false);
  });
});
