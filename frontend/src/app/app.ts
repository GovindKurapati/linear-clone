import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { IssueService } from './services/issue.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly issues = inject(IssueService);

  ngOnInit(): void {
    // Use the seeded ENG team's GUID from your Teams table
    this.issues.getByTeam('5a6fcfcf-d1b9-4398-9ae3-738d7c3db021').subscribe({
      next: (data) => console.log('Issues from API:', data),
      error: (err) => console.error('API error:', err),
    });
  }
}
