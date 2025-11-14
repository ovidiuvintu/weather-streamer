# Implementation Tasks: Delete Simulation

Branch: `001-delete-simulation`
Spec: `specs/001-delete-simulation/spec.md`

Priority: High — implement soft-delete with optimistic concurrency and audit entries.

Phases & Tasks

Phase 0 — Prep & Research (small tasks)
- T0.1: Ensure test infra seeds RowVersion for InMemory provider (small test helper). (Owner: dev)

Phase 1 — Schema & Repository
- T1.1: Add `IsDeleted` (bool) or `DeletedAt` (DateTime?) column to `Simulation` entity and EF configuration; mark default `false`.
- T1.2: Add EF Core migration to add column and configure concurrency token behavior (RowVersion unchanged). Generate SQL migration script for DB providers (SQLite and SQL Server notes).
- T1.3: Add global query filter in Infrastructure (`Simulation` DbSet) to exclude `IsDeleted = true` for public queries or update repository query methods accordingly.
- T1.4: Update repository layer: implement `SoftDeleteAsync(id, rowVersion)` that checks RowVersion concurrency and sets `IsDeleted=true` within a transaction; on concurrency conflict throw domain-specific concurrency exception.

Phase 2 — Application/Handler
- T2.1: Add `DeleteSimulationCommand` with properties: `Id`, `IfMatch` (string/base64 ETag), `Actor` (from context), `CorrelationId`.
- T2.2: Implement `DeleteSimulationHandler` to:
  - Validate `IfMatch` presence; decode base64 RowVersion.
  - Load simulation (include RowVersion) and check domain rules (disallow delete if `Running` or `Completed`). If illegal, throw `InvalidOperationException` with message starting `Illegal status transition` or `Cannot delete` to map to details.status.
  - Call repository `SoftDeleteAsync` with the decoded RowVersion.
  - Create and persist `AuditEntry` with `Action = "Delete"`, `ChangesJson` containing pre-delete snapshot, and `PrevEtag` base64(RowVersion).
  - Return success (no content) or throw concurrency exception mapped to 409.

Phase 3 — API Controller
- T3.1: Add `DELETE /api/simulations/{id}` endpoint in `SimulationsController`:
  - Require `If-Match` header (return 400 with details if missing).
  - Create `DeleteSimulationCommand` and send to mediator/handler.
  - Map exceptions to HTTP responses consistent with project conventions (ArgumentException -> 400 payload details; `InvalidOperationException` with message starting `Cannot delete` -> 400 with details.status; concurrency -> 409 with currentVersion).

Phase 4 — Tests
- T4.1: Unit tests for domain rule: deleting `Running`/`Completed` simulation throws expected domain error.
- T4.2: Unit tests for handler: missing If-Match -> ArgumentException; invalid base64 -> ArgumentException; successful flow -> calls repository and persists audit.
- T4.3: Integration tests:
  - T4.3.1: Happy path: create simulation (Draft), GET ETag, DELETE with If-Match -> 204, DB row exists with `IsDeleted=true`, AuditEntry persisted.
  - T4.3.2: Missing If-Match -> 400 details show `If-Match`.
  - T4.3.3: Stale If-Match -> 409 with `currentVersion`.
  - T4.3.4: Illegal state -> 400 with details.status.

Phase 5 — Migrations & CI
- T5.1: Add migration SQL to repo (non-idempotent SQLite script and guidance for SQL Server idempotent scripts) and include in PR.
- T5.2: Run `dotnet test` locally, ensure tests pass. Add any necessary test seeding helpers.

Phase 6 — Docs & PR
- T6.1: Update `specs/001-delete-simulation` with any final notes (already created research/data-model/contracts/quickstart).
- T6.2: Create PR from `001-delete-simulation` to `main` with title: `spec(delete): implement delete-simulation (soft-delete, audit)` and body referencing spec path.

Estimates (rough)
- Schema & Repo: 2–4 hours
- Handler & Controller: 2–3 hours
- Tests (unit + integration): 3–6 hours
- Migrations & docs: 1–2 hours

Definition of Done
- API `DELETE /api/simulations/{id}` implemented and returns correct status codes per acceptance criteria.
- Soft-delete stored (`IsDeleted=true`) and `AuditEntry` persisted.
- Tests (unit + integration) covering acceptance criteria present and passing.
- Migration script and plan included in PR.