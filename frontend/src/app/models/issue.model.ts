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
  sortKey: string;
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
  sortKey: string;
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
