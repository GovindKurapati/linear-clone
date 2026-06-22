import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Issue,
  IssueListItem,
  CreateIssueRequest,
  UpdateIssueRequest,
} from '../models/issue.model';

@Injectable({ providedIn: 'root' })
export class IssueService {
  // Modern Angular: inject() instead of constructor injection.
  private readonly http = inject(HttpClient);

  // Relative URL — the dev proxy forwards /api/* to the backend.
  private readonly baseUrl = '/api/issues';

  getByTeam(teamId: string): Observable<IssueListItem[]> {
    return this.http.get<IssueListItem[]>(`${this.baseUrl}/team/${teamId}`);
  }

  getById(id: string): Observable<Issue> {
    return this.http.get<Issue>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateIssueRequest): Observable<Issue> {
    return this.http.post<Issue>(this.baseUrl, request);
  }

  update(id: string, request: UpdateIssueRequest): Observable<Issue> {
    return this.http.put<Issue>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
