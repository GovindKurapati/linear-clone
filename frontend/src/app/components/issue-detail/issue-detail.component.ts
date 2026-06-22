import { Component, inject, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { IssueService } from '../../services/issue.service';
import { WorkflowState } from '../../models/workflow-state.model';
import { Issue, IssuePriority, UpdateIssueRequest } from '../../models/issue.model';

@Component({
  selector: 'app-issue-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './issue-detail.component.html',
})
export class IssueDetailComponent {
  private readonly fb = inject(FormBuilder);
  private readonly issueService = inject(IssueService);

  // The id of the issue to show. When it changes, we (re)load the full issue.
  readonly issueId = input.required<string>();
  readonly states = input.required<WorkflowState[]>();

  readonly updated = output<Issue>();
  readonly deleted = output<string>();
  readonly closed = output<void>();

  readonly issue = signal<Issue | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  // Set true when the server returns 409 — the issue changed under us.
  readonly conflict = signal(false);

  readonly priorities = Object.values(IssuePriority);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(255)]],
    description: [''],
    priority: [IssuePriority.NoPriority],
    estimate: [null as number | null],
    stateId: [''],
  });

  constructor() {
    // effect() re-runs whenever issueId() changes — load that issue.
    effect(() => {
      const id = this.issueId();
      if (id) this.load(id);
    });
  }

  private load(id: string): void {
    this.error.set(null);
    this.conflict.set(false);
    this.issueService.getById(id).subscribe({
      next: (issue) => {
        this.issue.set(issue);
        this.form.patchValue({
          title: issue.title,
          description: issue.description ?? '',
          priority: issue.priority,
          estimate: issue.estimate,
          stateId: issue.stateId,
        });
      },
      error: () => this.error.set('Could not load this issue.'),
    });
  }

  save(): void {
    const current = this.issue();
    if (!current || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.conflict.set(false);

    const v = this.form.getRawValue();
    const request: UpdateIssueRequest = {
      title: v.title.trim(),
      description: v.description?.trim() || null,
      priority: v.priority,
      estimate: v.estimate,
      stateId: v.stateId,
      // The crux of optimistic concurrency: send back the rowVersion we loaded.
      // If someone else edited the issue since, the server's value differs and
      // the update is rejected with 409.
      rowVersion: current.rowVersion,
    };

    this.issueService.update(current.id, request).subscribe({
      next: (issue) => {
        this.saving.set(false);
        this.issue.set(issue); // refreshes rowVersion to the new value
        this.updated.emit(issue);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (err.status === 409) {
          // The concurrency check fired. Tell the user plainly and let them reload.
          this.conflict.set(true);
        } else {
          this.error.set('Could not save changes. Try again.');
        }
      },
    });
  }

  reload(): void {
    this.load(this.issueId());
  }

  remove(): void {
    const current = this.issue();
    if (!current) return;
    this.issueService.delete(current.id).subscribe({
      next: () => this.deleted.emit(current.id),
      error: () => this.error.set('Could not delete this issue.'),
    });
  }

  close(): void {
    this.closed.emit();
  }
}
