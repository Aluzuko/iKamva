import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminDisputes } from './admin-disputes';

describe('AdminDisputes', () => {
  let component: AdminDisputes;
  let fixture: ComponentFixture<AdminDisputes>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDisputes],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminDisputes);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/disputes').flush([
      { disputeId: 'd1', raisedByName: 'Nomsa', subject: 'Missed lesson', description: 'Tutor never joined', status: 'open', resolution: null, createdAt: '2026-10-01', resolvedAt: null },
      { disputeId: 'd2', raisedByName: 'Sipho', subject: 'Double charge', description: 'Charged twice', status: 'resolved', resolution: 'Refunded', createdAt: '2026-09-01', resolvedAt: '2026-09-02' },
    ]);
  });

  afterEach(() => http.verify());

  it('shows open disputes first by default', () => {
    expect(component.filtered().map((d) => d.disputeId)).toEqual(['d1']);
  });

  it('requires an outcome and then resolves', () => {
    const d = component.disputes()[0];
    component.open(d, 'resolve');
    component.submit();
    http.expectNone('/api/admin/disputes/d1/resolve');
    component.form.setValue({ resolution: 'Refunded $25' });
    component.submit();
    http.expectOne('/api/admin/disputes/d1/resolve').flush(null);
    expect(component.disputes()[0].status).toBe('resolved');
    expect(component.filtered().length).toBe(0);
  });
});
