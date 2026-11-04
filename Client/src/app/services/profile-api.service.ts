import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Profile, UpdateRoleInfoPayload } from '../models/profile.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProfileApiService {
  private readonly baseUrl = `${environment.apiUrl}/profile`;
  private readonly authUrl = `${environment.apiUrl}/auth`;

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(this.baseUrl);
  }

  updateProfile(payload: UpdateRoleInfoPayload): Observable<void> {
    return this.http.put<void>(this.baseUrl, payload);
  }

  changeEmail(newEmail: string, currentPassword: string): Observable<void> {
    return this.http.post<void>(`${this.authUrl}/change-email`, { newEmail, currentPassword });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.authUrl}/change-password`, { currentPassword, newPassword });
  }
}
