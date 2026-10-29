import { Injectable } from '@angular/core';
import {
  HttpBackend,
  HttpClient,
  HttpEvent,
  HttpErrorResponse,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthResponse } from '../models/auth.model';
import { TokenStorageService } from '../services/token-storage.service';

const AUTH_ENDPOINT_PATTERN = /\/api\/auth\/(login|register|refresh|logout)$/;

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly rawHttp: HttpClient;
  private refreshing$: Observable<string> | null = null;

  constructor(
    private readonly tokenStorage: TokenStorageService,
    private readonly router: Router,
    backend: HttpBackend
  ) {
    this.rawHttp = new HttpClient(backend);
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (AUTH_ENDPOINT_PATTERN.test(req.url) || !this.tokenStorage.get()) {
      return next.handle(req);
    }

    const send = (token: string | null) => next.handle(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);

    const first$ = this.tokenStorage.accessTokenExpiring ? this.refresh().pipe(switchMap((t) => send(t))) : send(this.tokenStorage.accessToken);

    return first$.pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && this.tokenStorage.get()?.refreshToken) {
          return this.refresh().pipe(switchMap((token) => send(token)));
        }
        return throwError(() => error);
      })
    );
  }

  private refresh(): Observable<string> {
    if (!this.refreshing$) {
      const refreshToken = this.tokenStorage.get()?.refreshToken;
      this.refreshing$ = (refreshToken
        ? this.rawHttp.post<AuthResponse>('/api/auth/refresh', { refreshToken })
        : throwError(() => new HttpErrorResponse({ status: 401 }))
      ).pipe(
        map((response) => {
          this.tokenStorage.save(response);
          return response.accessToken;
        }),
        catchError((error: unknown) => {
          if (!(error instanceof HttpErrorResponse) || error.status !== 0) {
            this.tokenStorage.clear();
            this.router.navigate(['/login']);
          }
          return throwError(() => error);
        }),
        finalize(() => (this.refreshing$ = null)),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }
    return this.refreshing$;
  }
}
