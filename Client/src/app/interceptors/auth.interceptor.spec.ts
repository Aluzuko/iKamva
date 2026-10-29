import { TestBed } from '@angular/core/testing';
import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthInterceptor } from './auth.interceptor';
import { TokenStorageService } from '../services/token-storage.service';

const session = (accessMinutes: number) => ({
  accessToken: 'old',
  accessTokenExpiresAt: new Date(Date.now() + accessMinutes * 60000).toISOString(),
  refreshToken: 'r1',
  refreshTokenExpiresAt: new Date(Date.now() + 86400000).toISOString(),
  user: { id: 'u', email: 'a@b.c', fullName: 'A B', role: 'student' },
});

describe('AuthInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let storage: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '**', children: [] }]),
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(TokenStorageService);
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
  });

  it('refreshes once on 401 and retries with the new token', () => {
    storage.save(session(10) as never);
    let result: unknown;
    http.get('/api/student/lessons').subscribe((r) => (result = r));
    backend.expectOne('/api/student/lessons').flush({}, { status: 401, statusText: 'Unauthorized' });
    const refresh = backend.expectOne('/api/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'r1' });
    refresh.flush({ ...session(15), accessToken: 'new', refreshToken: 'r2' });
    const retry = backend.expectOne('/api/student/lessons');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new');
    retry.flush([1]);
    expect(result).toEqual([1]);
    expect(storage.get()?.refreshToken).toBe('r2');
  });

  it('refreshes before sending when the access token has expired', () => {
    storage.save(session(-1) as never);
    http.get('/api/tutor/lessons').subscribe();
    backend.expectOne('/api/auth/refresh').flush({ ...session(15), accessToken: 'fresh' });
    expect(backend.expectOne('/api/tutor/lessons').request.headers.get('Authorization')).toBe('Bearer fresh');
  });

  it('signs out when the refresh token is rejected', () => {
    storage.save(session(10) as never);
    http.get('/api/student/lessons').subscribe({ error: () => undefined });
    backend.expectOne('/api/student/lessons').flush({}, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/auth/refresh').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(storage.get()).toBeNull();
  });
});
