// Mirrors WorkflowStateDto from the backend.

export enum StateCategory {
  Backlog = 'Backlog',
  Unstarted = 'Unstarted',
  Started = 'Started',
  Completed = 'Completed',
  Canceled = 'Canceled',
}

export interface WorkflowState {
  id: string;
  teamId: string;
  name: string;
  sortOrder: number;
  category: StateCategory;
}
