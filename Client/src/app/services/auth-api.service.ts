import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginPayload, RegisterPayload } from '../models/auth.model';
import { TokenStorageService } from './token-storage.service';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  constructor(private readonly http: HttpClient, private readonly tokenStorage: TokenStorageService) {}

  login(payload: LoginPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, payload)
      .pipe(tap((response) => this.tokenStorage.save(response)));
  }

  register(payload: RegisterPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, payload)
      .pipe(tap((response) => this.tokenStorage.save(response)));
  }

  logout(): Observable<void> {
    const refreshToken = this.tokenStorage.get()?.refreshToken;
    this.tokenStorage.clear();
    if (!refreshToken) {
      return new Observable((subscriber) => subscriber.complete());
    }
    return this.http.post<void>(`${this.baseUrl}/logout`, { refreshToken });
  }
}
