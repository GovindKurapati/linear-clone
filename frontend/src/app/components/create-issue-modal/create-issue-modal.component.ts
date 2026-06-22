import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { IssueService } from '../../services/issue.service';
import { WorkflowState } from '../../models/workflow-state.model';
import { CreateIssueRequest, Issue, IssuePriority } from '../../models/issue.model';

@Component({
  selector: 'app-create-issue-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create-issue-modal.component.html',
})
export class CreateIssueModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly issueService = inject(IssueService);

  // Inputs from the parent (modern signal-based input()).
  readonly teamId = input.required<string>();
  readonly states = input.required<WorkflowState[]>();

  // Outputs: tell the parent an issue was created, or that the user closed.
  readonly created = output<Issue>();
  readonly closed = output<void>();

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly priorities = Object.values(IssuePriority);

  // Typed reactive form. nonNullable keeps values non-null for cleaner typing.
  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(255)]],
    description: [''],
    priority: [IssuePriority.NoPriority],
    estimate: [null as number | null],
    stateId: ['' as string], // empty = let the backend pick the default state
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    const request: CreateIssueRequest = {
      teamId: this.teamId(),
      title: v.title.trim(),
      description: v.description?.trim() || null,
      priority: v.priority,
      estimate: v.estimate,
      stateId: v.stateId || null,
      parentId: null,
    };

    this.issueService.create(request).subscribe({
      next: (issue) => {
        this.saving.set(false);
        this.created.emit(issue);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Could not create the issue. Try again.');
      },
    });
  }

  close(): void {
    this.closed.emit();
  }
}
