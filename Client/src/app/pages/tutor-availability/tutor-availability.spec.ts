import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TutorAvailability } from './tutor-availability';

describe('TutorAvailability', () => {
  let component: TutorAvailability;
  let fixture: ComponentFixture<TutorAvailability>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorAvailability],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(TutorAvailability);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/tutor/availability').flush({ timeZone: 'Africa/Johannesburg', slots: [{ dayOfWeek: 1, startMinute: 900, endMinute: 1080 }] });
  });

  afterEach(() => http.verify());

  it('loads slots into the right day', () => {
    expect(component.week()[1]).toEqual([{ start: '15:00', end: '18:00' }]);
    expect(component.totalHours()).toBe(3);
  });

  it('flags overlapping blocks and refuses to save', () => {
    component.addBlock(1);
    component.setTime(1, 1, 'start', '17:00');
    component.setTime(1, 1, 'end', '19:00');
    expect(component.problems()[1]).toBe('Some blocks overlap.');
    component.save();
    http.expectNone('/api/tutor/availability');
  });

  it('copies a weekday to Monday through Friday', () => {
    component.copyToWeekdays(1);
    expect(component.week()[5]).toEqual([{ start: '15:00', end: '18:00' }]);
    expect(component.week()[6]).toEqual([]);
    component.save();
    const req = http.expectOne('/api/tutor/availability');
    expect(req.request.body.slots.length).toBe(5);
    req.flush(null);
    expect(component.dirty()).toBe(false);
  });
});
