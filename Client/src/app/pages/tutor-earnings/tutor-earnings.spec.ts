import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TutorEarnings } from './tutor-earnings';

const earnings = (over: object = {}) => ({
  totalEarned: 500, availableBalance: 120, pendingPayouts: 0, paidOut: 380, thisMonth: 90, minimumPayout: 20,
  canRequestPayout: true, payoutBlockedReason: null, monthly: [{ label: 'Oct', value: 90 }], recentLessons: [], payouts: [], ...over,
});

describe('TutorEarnings', () => {
  let component: TutorEarnings;
  let fixture: ComponentFixture<TutorEarnings>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorEarnings],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorEarnings);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/earnings').flush(earnings());
  });

  afterEach(() => http.verify());

  it('requests a payout and switches to the payouts tab', () => {
    component.requestPayout();
    http.expectOne('/api/tutor/payouts').flush({ payoutId: 'p', amount: 120, status: 'requested', reference: null, requestedAt: '2026-10-22', paidAt: null });
    http.expectOne('/api/tutor/earnings').flush(earnings({ availableBalance: 0, pendingPayouts: 120, canRequestPayout: false }));
    expect(component.tab()).toBe('payouts');
    expect(component.data()?.pendingPayouts).toBe(120);
  });
});
