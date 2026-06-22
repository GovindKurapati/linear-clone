import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkflowState } from '../models/workflow-state.model';

@Injectable({ providedIn: 'root' })
export class WorkflowStateService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/workflowstates';

  getByTeam(teamId: string): Observable<WorkflowState[]> {
    return this.http.get<WorkflowState[]>(`${this.baseUrl}/team/${teamId}`);
  }
}
