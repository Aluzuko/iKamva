import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { PaymentResult } from './payment-result';
import { UserRole } from '../../models/user-role.model';

const attempt = (status: string, invoiceStatus = 'pending') => ({
  attemptId: 'a1', invoiceId: 'i1', status, invoiceStatus, amount: 620, description: 'October 2026 · 8 lessons', createdAt: '2026-10-22', completedAt: null,
});

function setup(query: Record<string, string>, role = UserRole.Parent) {
  TestBed.configureTestingModule({
    imports: [PaymentResult],
    providers: [
      provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            queryParamMap: convertToParamMap(query),
            pathFromRoot: [{ data: { role } }],
            data: {},
          },
          parent: null,
        },
      },
    ],
  });
  const fixture: ComponentFixture<PaymentResult> = TestBed.createComponent(PaymentResult);
  fixture.detectChanges();
  return { component: fixture.componentInstance, http: TestBed.inject(HttpTestingController) };
}

describe('PaymentResult', () => {
  afterEach(() => vi.useRealTimers());

  it('keeps checking until PayFast confirms', () => {
    vi.useFakeTimers();
    const { component, http } = setup({ attempt: 'a1' });
    http.expectOne('/api/parent/payments/attempts/a1').flush(attempt('pending'));
    expect(component.view()).toBe('checking');
    vi.advanceTimersByTime(2000);
    http.expectOne('/api/parent/payments/attempts/a1').flush(attempt('complete', 'paid'));
    expect(component.view()).toBe('paid');
    http.verify();
  });

  it('stops polling and shows waiting when confirmation stays pending', () => {
    vi.useFakeTimers();
    const { component, http } = setup({ attempt: 'a1' });

    for (let poll = 0; poll < 15; poll++) {
      http.expectOne('/api/parent/payments/attempts/a1').flush(attempt('pending'));
      vi.advanceTimersByTime(2000);
    }

    expect(component.view()).toBe('waiting');
    vi.advanceTimersByTime(60_000);
    http.expectNone('/api/parent/payments/attempts/a1');
    http.verify();
  });

  it('shows cancelled when PayFast sends the parent back', () => {
    const { component, http } = setup({ attempt: 'a1', cancelled: '1' });
    http.expectOne('/api/parent/payments/attempts/a1').flush(attempt('cancelled'));
    expect(component.view()).toBe('cancelled');
    http.verify();
  });

  it('still shows paid if the notification arrived before a cancel redirect', () => {
    const { component, http } = setup({ attempt: 'a1', cancelled: '1' });
    http.expectOne('/api/parent/payments/attempts/a1').flush(attempt('complete', 'paid'));
    expect(component.view()).toBe('paid');
    http.verify();
  });

  it('handles a missing attempt id', () => {
    const { component } = setup({});
    expect(component.view()).toBe('missing');
  });

  it('uses the student payment links for student checkouts', () => {
    const { component, http } = setup({}, UserRole.Student);
    expect(component.accountBase()).toBe('student');
    http.verify();
  });
});
