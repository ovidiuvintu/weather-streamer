# Delete Simulation (REST API)

Summary
- Provide a REST endpoint to delete a Simulation resource by id. The endpoint enforces optimistic concurrency and records an audit entry for deletions.

Owner: @team

Endpoints
- DELETE /api/simulations/{id}

Behavior
- Requires `If-Match` header containing the resource ETag (base64 rowversion) for optimistic concurrency.
- On success: returns `204 No Content` and persists an `AuditEntry` describing the deletion (including `PrevEtag`, `Actor`, `CorrelationId`, `TimestampUtc`).
- If the `If-Match` header is missing: return `400 Bad Request` with structured details `{ "If-Match": ["...required..."] }`.
- If the resource is not found: return `404 Not Found`.
- If the `If-Match` value does not match the current version: return `409 Conflict` with details including the current `ETag` (e.g., `currentVersion`).
- If the delete operation violates domain invariants (e.g., cannot delete a Running simulation): return `400 Bad Request` with structured details keyed by the failing property (e.g., `status`).

Security
- Requires authentication. The actor performing the delete must be recorded in `AuditEntry.Actor`.
- Authorization rules: only owners or users with `Simulation.Delete` permission may delete.

Audit
- Persist an `AuditEntry` with fields: `SimulationId`, `Actor`, `CorrelationId`, `TimestampUtc`, `Action:"Delete"`, `ChangesJson` (should capture previous state), `PrevEtag`.

Acceptance Criteria
- DELETE with valid `If-Match` on an existing simulation in `Draft` or `Stopped` returns `204` and creates an audit entry.
- DELETE without `If-Match` returns `400` with details for `If-Match`.
- DELETE for non-existing id returns `404`.
- DELETE with stale `If-Match` returns `409` and includes `currentVersion` detail.
- DELETE of a simulation in `Running` or `Completed` returns `400` with details `{ "status": ["...reason..."] }`.
- Unit tests cover domain rule rejection and repository concurrency; integration tests exercise the full controller -> handler path including audit persistence.

Notes
- Reuse existing RowVersion -> ETag encoding (base64) used elsewhere in the API.
- Follow existing error mapping conventions (`ArgumentException` -> 400 with details, domain `InvalidOperationException` -> 400 with `status` if message starts with "Illegal status transition", concurrency -> 409).

Related specs
- `specs/003-update-simulations/spec.md` (concurrency, ETag semantics, audit format)
