# Linear Clone — Full-Stack Build Roadmap

**Stack:** ASP.NET Core (.NET 8) Web API · SQL Server · Angular · EF Core
**Architecture:** Vertical Slice + CQRS (MediatR) + FluentValidation
**Machine:** MacBook Air M3 · SQL Server 2022 in Docker (Rosetta) · VS Code MSSQL extension
**Pace:** ~5–8 hrs/week · Target: demoable app at the end of every phase
**Cloud:** Deferred — everything runs locally until you choose to deploy

> How to use this file: keep it as `ROADMAP.md` in your repo root. Check boxes off as you go (`- [x]`). Each phase ends with a **Deliverable** — a working, committed state you can stop at. The **"Next 3 tasks"** note at the bottom is your anti-drift tool: update it at the end of every session so you never lose a session to re-orientation.

---

## Guiding principles

- [x] End every session with something **committed and working** — never leave the repo half-broken
- [ ] Stay in **LINQ/EF Core** (no raw T-SQL) through Phase 5 to keep the DB swappable; commit to SQL-Server-specifics deliberately in Phase 6+
- [ ] Keep the frontend **clean-but-functional** — resist Linear's pixel-polish rabbit hole; your mastery payoff is backend depth
- [ ] Be able to **articulate the "why"** behind each architectural choice — that's what enterprise interviews probe

---

## Phase 0 — Setup & Scaffold _(Week 1, ~6 hrs)_

**Goal:** A containerized empty app that builds, runs, and talks frontend↔backend.

### Environment

- [ ] Install/confirm latest **Docker Desktop**; enable Settings → General → "Use Rosetta for x86/amd64 emulation"
- [ ] Install **.NET 8 SDK** (`dotnet --version` confirms)
- [ ] Install **Node.js LTS** + **Angular CLI** (`npm i -g @angular/cli`)
- [ ] Install **VS Code** + extensions: C# Dev Kit, **MSSQL**, Angular Language Service
- [ ] Bring up **SQL Server 2022** via `docker-compose.yml` (see code I gave you), confirm connection from the MSSQL extension

### Backend skeleton

- [ ] `dotnet new sln -n LinearClone`
- [ ] Create projects: `Api` (web), `Application`, `Domain`, `Infrastructure` + `Api.Tests`
- [ ] Wire project references (Api → Application → Domain; Infrastructure → Application)
- [ ] Add NuGet packages: MediatR, FluentValidation, EF Core + SqlServer provider, Serilog
- [ ] Add a `GET /health` endpoint that returns OK
- [ ] Configure Serilog request logging
- [ ] First EF Core `DbContext` (empty) + connection string in `appsettings.Development.json`

### Frontend skeleton

- [ ] `ng new linear-web` (standalone components, routing, SCSS)
- [ ] Add Angular Material **or** PrimeNG (pick one, note the choice)
- [ ] Configure environment file with API base URL + a proxy for local dev (avoid CORS pain)
- [ ] One page that calls `/health` and renders the result

### Repo hygiene

- [ ] `git init`, sensible `.gitignore` (.NET + Node), first commit
- [ ] Add this `ROADMAP.md` and a short `README.md`

**✅ Deliverable:** Empty app — Angular page shows backend health status; SQL Server running in Docker; all committed.

---

## Phase 1 — Core Domain, Single-Player _(Weeks 2–5, ~28 hrs)_

**Goal:** Manage issues in the browser. One hardcoded user, no auth yet.

### Domain & data

- [x] Entities: `Team`, `WorkflowState` (customizable status, ordered), `Issue` (title, description, priority, estimate, state FK, created/updated)
- [x] Self-referencing `Issue.ParentId` for sub-issues
- [x] EF Core configurations (Fluent API) for each entity
- [x] First migration + apply to SQL Server; verify tables via MSSQL extension
- [x] Seed data: one team, a default set of workflow states, a few issues

### Vertical slices (CQRS)

- [x] `CreateIssue` command + validator + handler
- [x] `GetIssues` query (by team, with state)
- [x] `GetIssueById` query
- [x] `UpdateIssue` command
- [x] `DeleteIssue` command
- [x] Map endpoints (Minimal API or controllers — pick one, note why)
- [x] DTOs separate from domain entities

### Frontend

- [x] API client service (typed)
- [x] Issue list view (table/list, grouped by state)
- [x] Issue detail view + edit form
- [x] Create-issue flow
- [x] Delete with confirm

### Tests

- [x] Unit-test one or two handlers (validation + happy path)

**✅ Deliverable:** You can create, view, edit, and delete issues in the browser, persisted to SQL Server.

---

## Phase 2 — The Board + Ordering _(Weeks 6–8, ~21 hrs)_

**Goal:** A fast, draggable Kanban board with correct ordering.

- [x] Add `Issue.SortKey` (string) for **fractional indexing** — study LexoRank concept first
- [x] Implement a fractional-index helper (generate key between two keys)
- [x] `ReorderIssue` command (takes before/after neighbor keys → new key)
- [x] Board view in Angular using **`@angular/cdk/drag-drop`**
- [x] Drag between columns (changes state) and within a column (changes order)
- [x] **Optimistic UI**: update locally first, reconcile with server response, roll back on error
- [x] Handle the "drop at top/bottom of empty column" edge cases

**✅ Deliverable:** A draggable board where reordering one card doesn't rewrite the whole column.

---

## Phase 3 — Auth & Multi-Tenancy _(Weeks 9–11, ~21 hrs)_

**Goal:** Real login; data isolated per workspace.

- [ ] Add `Workspace` (tenant) + `User` + `Membership` (user↔workspace, with role)
- [ ] Choose auth approach (ASP.NET Core Identity **or** JWT) — note the tradeoff you picked
- [ ] Registration + login endpoints; issue tokens
- [ ] **EF Core global query filters** for tenant isolation (every query auto-scoped to workspace)
- [ ] Resolve current user + workspace from the request (middleware / claims)
- [ ] Role-based authorization on endpoints
- [ ] Angular: login page, token storage, auth interceptor, route guards
- [ ] Re-test Phases 1–2 flows now that everything is tenant-scoped

**✅ Deliverable:** Genuinely demoable multi-tenant app — **first natural stopping point for the job search.**

---

## Phase 3.5 — First Deploy _(optional, when credits exist, ~6–8 hrs)_

**Goal:** Get cloud config pain out of the way early, in small doses.

- [ ] Azure account + free tier / credits sorted
- [ ] Azure SQL Database (same engine as local)
- [ ] Azure App Service for the API
- [ ] Static hosting for the Angular build
- [ ] Production connection strings + secrets handled (not in source)
- [ ] Live URL works end-to-end

**✅ Deliverable:** A live URL you can put in front of recruiters months before the project is "done."

---

## Phase 4 — Activity Log & Comments _(Weeks 12–13, ~14 hrs)_

**Goal:** Every change is tracked; issues have discussion.

- [ ] `ActivityLog` entity (append-only: actor, issue, change type, old→new)
- [ ] Capture changes automatically via **EF Core SaveChanges interceptor** _or_ domain events — don't litter write code
- [ ] Log: status changed, assignee changed, priority changed, created
- [ ] `Comment` entity + create/list slices
- [ ] Frontend: activity stream + comments on the issue detail view

**✅ Deliverable:** Issue detail shows a full, auto-generated history plus comments.

---

## Phase 5 — Real-Time Sync _(Weeks 14–16, ~21 hrs)_

**Goal:** The "feels like Linear" moment — changes broadcast live.

- [ ] Add **SignalR** hub; group connections by workspace
- [ ] Broadcast issue create/update/move/delete to the workspace group
- [ ] Angular SignalR client; apply incoming changes to local state
- [ ] Handle **reconnection** (re-sync on reconnect) and basic **presence**
- [ ] Reconcile concurrent edits sensibly (last-write-wins is fine to start — note the limitation)
- [ ] Test: two browser tabs, change one, watch the other update

**✅ Deliverable:** Multi-client live sync — **portfolio-strong stopping point.**

---

## Phase 6 — Search & Filtering _(Weeks 17–18, ~14 hrs)_

**Goal:** Instant search and filtering. _(First deliberate SQL-Server-specific work.)_

- [ ] Enable **SQL Server Full-Text Search** in the container; create catalog + index via migration
- [ ] Search endpoint across issue title/description
- [ ] Filter/query layer: by assignee, label, state, cycle (composable)
- [ ] Add `Label` entity + issue↔label many-to-many
- [ ] Frontend: filter bar + a **command-palette** style quick search
- [ ] (Optional) Inspect an execution plan for the search query — interview gold

**✅ Deliverable:** Instant full-text search and multi-criteria filtering.

---

## Phase 7 — Cycles & Background Jobs _(Weeks 19–20, ~14 hrs)_

**Goal:** Sprints that roll over automatically.

- [ ] `Cycle` entity (time-boxed sprint) + assign issues to cycles
- [ ] Add **Hangfire** (SQL Server storage)
- [ ] Recurring job: roll incomplete issues into the next cycle
- [ ] Make the job **idempotent** (safe to run twice)
- [ ] Notifications on assignment / cycle change
- [ ] Frontend: cycle view + progress rollup

**✅ Deliverable:** Cycles that auto-rollover on schedule.

---

## Phase 8 — Hardening & Deploy _(Weeks 21–24, ~24 hrs)_

**Goal:** Tested, polished, recruiter-shareable.

- [ ] Integration tests with **`WebApplicationFactory`** + **Testcontainers** (real SQL Server in tests)
- [ ] Cover the critical flows: auth, create/move issue, tenant isolation
- [ ] **Redis** caching for hot reads (or in-memory if deferring Redis)
- [ ] **Rate limiting** on the API
- [ ] **CI/CD** with GitHub Actions (build, test, deploy)
- [ ] Full Azure deploy if not done at 3.5 (App Service + Azure SQL + SignalR Service + Blob for attachments)
- [ ] README with architecture diagram + screenshots + live link
- [ ] Polish pass on the UI

**✅ Deliverable:** Tested, deployed, documented — the full enterprise story.

---

## .NET mastery checklist (what this project proves you can do)

- [ ] Vertical Slice architecture + CQRS with MediatR
- [ ] FluentValidation
- [ ] EF Core: migrations, Fluent config, global query filters, interceptors, concurrency tokens
- [ ] Multi-tenancy
- [ ] SignalR at non-trivial scale
- [ ] Fractional indexing
- [ ] Background jobs + idempotency (Hangfire)
- [ ] SQL Server full-text search + execution-plan reading
- [ ] Integration testing (WebApplicationFactory + Testcontainers)
- [ ] CI/CD + cloud deploy

---

## Next 3 tasks _(update at the END of every session)_

1. ***
2. ***
3. ***

**Last session left off at:** **\*\*\*\***\_\_\_\_**\*\*\*\***
**Current phase:** Phase 0
