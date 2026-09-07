import { Injectable } from '@angular/core';
import { FamilyDashboard } from '../models/parent.model';
import { Observable } from 'rxjs/internal/Observable';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class Admin {
   private readonly baseUrl = '/api/admin';

  constructor(private readonly http: HttpClient) {}

  getDashboard(): Observable<FamilyDashboard> {
      return this.http.get<FamilyDashboard>(`${this.baseUrl}/dashboard`);
    } 
}
