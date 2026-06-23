// Mirrors the backend C# enums/DTOs. Keep these in sync with Application/Issues.

export enum IssuePriority {
  NoPriority = 'NoPriority',
  Urgent = 'Urgent',
  High = 'High',
  Medium = 'Medium',
  Low = 'Low',
}

// Mirrors IssueListItemDto — the lightweight projection for list/board views.
export interface IssueListItem {
  id: string;
  stateId: string;
  number: number;
  identifier: string; // "ENG-123"
  title: string;
  priority: IssuePriority;
  estimate: number | null;
  position: number;
  rowVersion: string; // needed so the board can issue reorder calls
}

// Mirrors IssueDto — the full issue, including the concurrency token.
export interface Issue {
  id: string;
  teamId: string;
  stateId: string;
  parentId: string | null;
  number: number;
  identifier: string;
  title: string;
  description: string | null;
  priority: IssuePriority;
  estimate: number | null;
  position: number;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
  rowVersion: string; // base64-encoded byte[] from SQL Server rowversion
}

// Mirrors CreateIssueRequest.
export interface CreateIssueRequest {
  teamId: string;
  title: string;
  description: string | null;
  priority: IssuePriority;
  estimate: number | null;
  stateId: string | null;
  parentId: string | null;
}

// Mirrors UpdateIssueRequest. rowVersion must be the value last received from the
// server — this is what drives the optimistic-concurrency check.
export interface UpdateIssueRequest {
  title: string;
  description: string | null;
  priority: IssuePriority;
  estimate: number | null;
  stateId: string;
  rowVersion: string;
}

// Mirrors ReorderIssueRequest. Neighbor ids define where the card landed;
// either is null at a column edge.
export interface ReorderIssueRequest {
  targetStateId: string;
  beforeIssueId: string | null; // issue directly above the drop (null = top)
  afterIssueId: string | null; // issue directly below the drop (null = bottom)
  rowVersion: string;
}
