import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { IssueListComponent } from './components/issue-list/issue-list.component';
import { IssueBoardComponent } from './components/issue-board/issue-board.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', component: IssueListComponent, canActivate: [authGuard] },
  { path: 'board', component: IssueBoardComponent, canActivate: [authGuard] },
];
