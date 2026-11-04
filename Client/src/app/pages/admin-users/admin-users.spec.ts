import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminUsers } from './admin-users';

describe('AdminUsers', () => {
  let component: AdminUsers;
  let fixture: ComponentFixture<AdminUsers>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminUsers],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(AdminUsers);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/users').flush([
      { userId: 'u1', fullName: 'Sipho', email: 's@x.com', role: 'student', isActive: true, createdAt: '2026-01-01' },
      { userId: 'u2', fullName: 'Nomsa', email: 'n@x.com', role: 'parent', isActive: false, createdAt: '2026-01-01' },
      { userId: 'u3', fullName: 'Tutor', email: 't@x.com', role: 'tutor', isActive: true, createdAt: '2026-01-01' },
    ]);
  });

  afterEach(() => http.verify());

  it('only lists students and parents', () => {
    expect(component.people().length).toBe(2);
    expect(component.suspendedCount()).toBe(1);
  });

  it('asks before suspending and reactivates straight away', () => {
    const [student, parent] = component.people();
    component.toggle(student);
    expect(component.suspendTarget()).toBe(student);
    http.expectNone('/api/admin/users/u1/suspend');
    component.toggle(parent);
    http.expectOne('/api/admin/users/u2/activate').flush(null);
    expect(component.people()[1].isActive).toBe(true);
  });
});
