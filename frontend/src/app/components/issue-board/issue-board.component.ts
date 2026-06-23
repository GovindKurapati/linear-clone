import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CdkDragDrop,
  CdkDropList,
  CdkDrag,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { HttpErrorResponse } from '@angular/common/http';
import { IssueService } from '../../services/issue.service';
import { WorkflowStateService } from '../../services/workflow-state.service';
import { IssueListItem, IssuePriority, ReorderIssueRequest } from '../../models/issue.model';
import { WorkflowState } from '../../models/workflow-state.model';

interface Column {
  state: WorkflowState;
  issues: IssueListItem[];
}

@Component({
  selector: 'app-issue-board',
  standalone: true,
  imports: [CommonModule, CdkDropList, CdkDrag],
  templateUrl: './issue-board.component.html',
  styleUrl: './issue-board.component.scss',
})
export class IssueBoardComponent implements OnInit {
  private readonly issueService = inject(IssueService);
  private readonly stateService = inject(WorkflowStateService);

  readonly teamId = '5a6fcfcf-d1b9-4398-9ae3-738d7c3db021';

  readonly states = signal<WorkflowState[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  // The board holds its own mutable column arrays (CDK drag-drop needs real arrays
  // to move items between). We keep them as a signal of columns.
  readonly columns = signal<Column[]>([]);

  // Connected drop-list ids so CDK allows dragging between columns.
  readonly dropListIds = computed(() => this.columns().map((c) => 'list-' + c.state.id));

  readonly Priority = IssuePriority;

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.stateService.getByTeam(this.teamId).subscribe({
      next: (states) => {
        this.states.set(states);
        this.issueService.getByTeam(this.teamId).subscribe({
          next: (issues) => {
            // Group issues into columns by state (issues arrive already ordered).
            this.columns.set(
              states.map((state) => ({
                state,
                issues: issues.filter((i) => i.stateId === state.id),
              })),
            );
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Could not load issues.');
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.error.set('Could not load workflow states.');
        this.loading.set(false);
      },
    });
  }

  priorityColor(priority: IssuePriority): string {
    switch (priority) {
      case IssuePriority.Urgent:
        return 'bg-red-500';
      case IssuePriority.High:
        return 'bg-orange-400';
      case IssuePriority.Medium:
        return 'bg-yellow-400';
      case IssuePriority.Low:
        return 'bg-blue-400';
      default:
        return 'bg-zinc-600';
    }
  }

  // The heart of the board: translate CDK's drop event into a server reorder call,
  // optimistically. CDK tells us the source/target lists and indices; we move the
  // card in the local arrays immediately (so the UI feels instant), then derive the
  // before/after neighbor ids at the drop position and PATCH the server. If the
  // server rejects (e.g. 409 concurrency), we reload to restore truth.
  drop(event: CdkDragDrop<IssueListItem[]>, targetColumn: Column): void {
    // Snapshot for rollback.
    const snapshot = this.columns().map((c) => ({ ...c, issues: [...c.issues] }));

    if (event.previousContainer === event.container) {
      // Reordered within the same column.
      moveItemInArray(targetColumn.issues, event.previousIndex, event.currentIndex);
    } else {
      // Moved between columns.
      transferArrayItem(
        event.previousContainer.data,
        targetColumn.issues,
        event.previousIndex,
        event.currentIndex,
      );
    }
    // Trigger change detection by replacing the columns array reference.
    this.columns.set([...this.columns()]);

    // The dragged issue is now at currentIndex in the target column.
    const moved = targetColumn.issues[event.currentIndex];
    const before = targetColumn.issues[event.currentIndex - 1] ?? null; // above
    const after = targetColumn.issues[event.currentIndex + 1] ?? null; // below

    const request: ReorderIssueRequest = {
      targetStateId: targetColumn.state.id,
      beforeIssueId: before ? before.id : null,
      afterIssueId: after ? after.id : null,
      rowVersion: moved.rowVersion,
    };

    this.issueService.reorder(moved.id, request).subscribe({
      next: (updated) => {
        // Sync the moved card's authoritative position/rowVersion from the server.
        moved.position = updated.position;
        moved.stateId = updated.stateId;
        moved.rowVersion = updated.rowVersion;
      },
      error: (err: HttpErrorResponse) => {
        // Roll back to the snapshot; reload for the truthful order.
        this.columns.set(snapshot);
        this.error.set(
          err.status === 409
            ? 'That issue changed while you were moving it — reloading.'
            : 'Could not move the issue — reloading.',
        );
        this.load();
      },
    });
  }
}
