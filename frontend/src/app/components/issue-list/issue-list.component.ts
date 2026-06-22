import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IssueService } from '../../services/issue.service';
import { WorkflowStateService } from '../../services/workflow-state.service';
import { Issue, IssueListItem, IssuePriority } from '../../models/issue.model';
import { WorkflowState } from '../../models/workflow-state.model';
import { CreateIssueModalComponent } from '../create-issue-modal/create-issue-modal.component';
import { IssueDetailComponent } from '../issue-detail/issue-detail.component';

// A state paired with the issues that belong to it — the shape the template renders.
interface StateGroup {
  state: WorkflowState;
  issues: IssueListItem[];
}

@Component({
  selector: 'app-issue-list',
  standalone: true,
  imports: [CommonModule, CreateIssueModalComponent, IssueDetailComponent],
  templateUrl: './issue-list.component.html',
})
export class IssueListComponent implements OnInit {
  private readonly issueService = inject(IssueService);
  private readonly stateService = inject(WorkflowStateService);

  readonly showCreate = signal(false);
  readonly selectedIssueId = signal<string | null>(null);

  // TODO: replace with the real selected team once teams UI exists.
  // Use the seeded ENG team's GUID from your Teams table.
  private readonly teamId = '5a6fcfcf-d1b9-4398-9ae3-738d7c3db021';

  // Signals hold the raw fetched data. In a zoneless app these drive change detection.
  readonly issues = signal<IssueListItem[]>([]);
  readonly states = signal<WorkflowState[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  // computed() derives the grouped view from the two source signals. It re-runs
  // automatically whenever issues or states change — no manual wiring.
  readonly groups = computed<StateGroup[]>(() => {
    const issues = this.issues();
    return this.states().map((state) => ({
      state,
      issues: issues.filter((i) => i.stateId === state.id),
    }));
  });

  // Expose the enum to the template for priority styling.
  readonly Priority = IssuePriority;

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    // Fetch states and issues. For now two separate subscriptions; we can switch to
    // forkJoin later to coordinate them. States first so columns exist to group into.
    this.stateService.getByTeam(this.teamId).subscribe({
      next: (states) => this.states.set(states),
      error: () => this.error.set('Could not load workflow states.'),
    });

    this.issueService.getByTeam(this.teamId).subscribe({
      next: (issues) => {
        this.issues.set(issues);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load issues.');
        this.loading.set(false);
      },
    });
  }

  // Tailwind classes for the priority dot — priority is the one color accent.
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

  onCreated(issue: Issue): void {
    this.issues.update((list) => [
      ...list,
      {
        id: issue.id,
        stateId: issue.stateId,
        number: issue.number,
        identifier: issue.identifier,
        title: issue.title,
        priority: issue.priority,
        estimate: issue.estimate,
        sortKey: issue.sortKey,
      },
    ]);
    this.showCreate.set(false);
  }

  // when an issue is edited: refresh that row
  onUpdated(issue: Issue): void {
    this.issues.update((list) =>
      list.map((i) =>
        i.id === issue.id
          ? {
              ...i,
              title: issue.title,
              priority: issue.priority,
              estimate: issue.estimate,
              stateId: issue.stateId,
            }
          : i,
      ),
    );
  }

  // when deleted: remove it (it's soft-deleted server-side)
  onDeleted(id: string): void {
    this.issues.update((list) => list.filter((i) => i.id !== id));
    this.selectedIssueId.set(null);
  }
}
