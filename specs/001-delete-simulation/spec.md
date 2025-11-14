# Delete Simulation (REST API)

Short name: delete-simulation

Summary
- Add a RESTful DELETE endpoint to remove a Simulation resource by id. Deletion requires optimistic concurrency (If-Match/ETag) and records an audit entry describing the removed resource.

Owner: @team

Actors
- Authenticated user (actor) with `Simulation.Delete` permission or owner of the Simulation.

Endpoints
- DELETE /api/simulations/{id}

Behavior
- Requires the `If-Match` header set to the resource ETag (base64-encoded RowVersion) to enforce optimistic concurrency.
- Success response: `204 No Content`.
- On successful deletion, persist an `AuditEntry` with fields: `SimulationId`, `Actor`, `CorrelationId`, `TimestampUtc`, `Action: "Delete"`, `ChangesJson` (previous resource snapshot), `PrevEtag`.
- If `If-Match` header is missing: respond `400 Bad Request` with structured details: `{ "If-Match": ["The If-Match header is required for concurrency control."] }`.
- If the resource does not exist: respond `404 Not Found`.
- If `If-Match` does not match current RowVersion: respond `409 Conflict` with details `{ "If-Match": ["The provided version is stale."], "currentVersion": "<base64-current>" }`.
- If deletion violates domain rules (e.g., cannot delete a `Running` or `Completed` simulation): respond `400 Bad Request` with details keyed by the failing property (e.g., `{ "status": ["Cannot delete a Running simulation."] }`).

## Clarifications
### Session 2025-11-13
- Q: Delete behavior — hard vs soft? → A: Soft delete (mark `IsDeleted` flag on the Simulation row)

Behavior (updated)
- Deletions use a soft-delete strategy: the API will mark the Simulation's `IsDeleted` flag (or `Deleted` timestamp) and keep the row in the primary table. The row is retained for audit, recovery, and reporting. API listing/get operations treat soft-deleted resources as not found (404) by default.
- Requires the `If-Match` header set to the resource ETag (base64-encoded RowVersion) to enforce optimistic concurrency.
- Success response: `204 No Content`.
- On successful deletion, persist an `AuditEntry` with fields: `SimulationId`, `Actor`, `CorrelationId`, `TimestampUtc`, `Action: "Delete"`, `ChangesJson` (previous resource snapshot), `PrevEtag`.
- If `If-Match` header is missing: respond `400 Bad Request` with structured details: `{ "If-Match": ["The If-Match header is required for concurrency control."] }`.
- If the resource does not exist: respond `404 Not Found`.
- If `If-Match` does not match current RowVersion: respond `409 Conflict` with details `{ "If-Match": ["The provided version is stale."], "currentVersion": "<base64-current>" }`.
- If deletion violates domain rules (e.g., cannot delete a `Running` or `Completed` simulation): respond `400 Bad Request` with details keyed by the failing property (e.g., `{ "status": ["Cannot delete a Running simulation."] }`).

Security & Authorization
- Requires authentication; the actor performing the delete must be recorded in `AuditEntry.Actor`.
- Authorization rule: only the Simulation owner or users with `Simulation.Delete` permission may delete.

Audit
- On successful deletion, write an `AuditEntry` capturing the previous state (as `ChangesJson`), the `PrevEtag`, `Actor`, `CorrelationId`, and `TimestampUtc`.

User Scenarios
- Happy path: Owner issues `DELETE /api/simulations/12` with valid `If-Match` → 204; audit persisted.
- Missing If-Match: User issues `DELETE` without header → 400 with details `If-Match`.
- Not found: User issues `DELETE` for unknown id → 404.
- Stale version: User issues `DELETE` with old ETag → 409 with `currentVersion`.
- Illegal state: User attempts to delete a `Running` simulation → 400 with `{ "status": ["...reason..."] }`.

Functional Requirements (testable)
1. Endpoint accepts HTTP DELETE at `/api/simulations/{id}` and extracts `If-Match` header.
2. If `If-Match` is absent, API returns 400 with details containing `If-Match`.
3. If the simulation id is missing from the database, API returns 404.
4. Handler decodes the base64 `If-Match` into a RowVersion and passes it to repository delete call.
5. Repository performs a concurrency-protected delete (EF Core concurrency token or equivalent). If concurrency conflict occurs, the handler throws a concurrency exception mapped to 409 and includes current version in response details.
6. Domain rules prevent deletion of simulations in disallowed states (`Running`, `Completed`); these violations map to 400 with details keyed by `status`.
7. On success, persist an `AuditEntry` describing the deletion (previous state + PrevEtag) and return 204.
9. Repository must implement soft-delete semantics: update the Simulation's `IsDeleted` flag (or `DeletedAt` timestamp) instead of physically removing the row. Queries used by public APIs must exclude soft-deleted rows by default.
10. Integration tests should verify that soft-delete leaves the Simulation row present with `IsDeleted=true` and that `AuditEntry` references the deleted Simulation.
8. Unit tests exist to validate domain rule rejection and repository concurrency failure mapping. Integration tests cover controller → handler → repository → audit persistence flow.

Success Criteria
- Developers can run an integration test that creates a Simulation in `Draft`, issues `DELETE` with correct `If-Match`, and observes a 204 and an `AuditEntry` persisted referencing the deleted Simulation.
- Deleting without `If-Match` returns 400 with clear details.
- Attempting to delete with a stale `If-Match` returns 409 and includes `currentVersion`.
- Deleting a `Running` or `Completed` simulation returns 400 with `status` details indicating reason.

Key Entities
- Simulation: { Id, Name, StartTime, FileName, Status, RowVersion }
- AuditEntry: { Id, SimulationId, Actor, CorrelationId, TimestampUtc, Action, ChangesJson, PrevEtag }

Updated Entities
- Simulation: { Id, Name, StartTime, FileName, Status, RowVersion, IsDeleted }
- AuditEntry: { Id, SimulationId, Actor, CorrelationId, TimestampUtc, Action, ChangesJson, PrevEtag }

Assumptions
- ETag is base64(RowVersion) and existing encode/decode helpers are reused.
- Authorization is enforced by an existing middleware or attribute; the handler will still validate ownership/permission and return 403 if unauthorized (outside of this spec's acceptance tests, which focus on behavior when authorized).
- CorrelationId is available from request context and is included in created `AuditEntry`.
- Soft-delete approach is acceptable for this project and preserves rows for audit and recovery; downstream storage and reporting consumers will be adjusted to ignore `IsDeleted` rows by default.

Acceptance Tests (high level)
- DELETE valid: create simulation (Draft) → GET ETag → DELETE with If-Match → expect 204 and an AuditEntry present with PrevEtag matching provided ETag.
- DELETE valid (soft-delete): create simulation (Draft) → GET ETag → DELETE with If-Match → expect 204; the Simulation row remains in DB with `IsDeleted=true`; an `AuditEntry` is persisted referencing the Simulation and PrevEtag.
- Missing If-Match: DELETE without If-Match → 400 and details include `If-Match`.
- Stale version: after updating simulation to change RowVersion, previous ETag used in DELETE → 409 and details include `currentVersion`.
- Illegal state: create simulation in `Running` → DELETE with correct If-Match → 400 with details.status.

Notes
- Reuse the same error mapping conventions employed in `specs/003-update-simulations`.
- Tests should use InMemory provider where appropriate but seed RowVersion where necessary to avoid provider inconsistencies.

Related specs
- `specs/003-update-simulations/spec.md` (concurrency, ETag semantics, audit format)
