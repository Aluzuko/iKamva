import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Register } from './register';
import { UserRole } from '../../models/user-role.model';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not offer the admin role', () => {
    expect(component.roles.map((r) => r.role)).not.toContain(UserRole.Admin);
  });

  it('rejects passwords that do not meet the rules', () => {
    component.form.patchValue({ password: 'weakpass' });
    expect(component.form.get('password')?.valid).toBe(false);
    component.form.patchValue({ password: 'Abcdefg1' });
    expect(component.form.get('password')?.valid).toBe(true);
  });

  it('flags a duplicate email when the API returns 409', () => {
    component.form.setValue({
      role: UserRole.Parent,
      fullName: 'Maria Thompson',
      email: 'maria@example.com',
      password: 'Abcdefg1',
      confirmPassword: 'Abcdefg1',
    });
    component.submit();
    http.expectOne('/api/auth/register').flush({}, { status: 409, statusText: 'Conflict' });
    expect(component.emailTaken()).toBe(true);
  });
});
