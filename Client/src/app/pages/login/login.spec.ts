import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not call the API when the form is invalid', () => {
    component.submit();
    http.expectNone('/api/auth/login');
    expect(component.form.touched).toBe(true);
  });

  it('opens the dashboard for the role the API returns', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    component.form.setValue({ email: 'maria@example.com', password: 'Demo1234!' });
    component.submit();

    http.expectOne('/api/auth/login').flush({
      accessToken: 't',
      accessTokenExpiresAt: new Date(Date.now() + 60000).toISOString(),
      refreshToken: 'r',
      refreshTokenExpiresAt: new Date(Date.now() + 60000).toISOString(),
      user: { id: '1', email: 'maria@example.com', fullName: 'Maria Thompson', role: 'parent' },
    });

    expect(navigate).toHaveBeenCalledWith(['/', 'parent', 'dashboard']);
  });

  it('shows a clear message for wrong credentials', () => {
    component.form.setValue({ email: 'maria@example.com', password: 'wrong' });
    component.submit();
    http.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(component.errorMessage()).toBe('The email or password is incorrect.');
  });
});
