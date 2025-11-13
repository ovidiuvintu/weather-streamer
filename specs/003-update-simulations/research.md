# Research: Simulation Update Feature

## Decisions & Rationale

### 1. Concurrency Token Shape
- **Decision**: Use an `ETag` header exposing a base64-encoded rowversion (byte[]) from EF Core. Clients MUST send `If-Match: <ETag>` on PATCH. Response will include updated ETag.
- **Rationale**: Standard HTTP conditional semantics reduce payload coupling and leverage existing caching/proxy mechanisms; avoids embedding version field inside JSON body.
- **Alternatives Considered**:
  - Body `version` field: Simpler but mixes transport and domain concerns; less tooling support.
  - Incrementing integer version: Works but rowversion provides built-in atomicity in SQL engines.

### 2. Actor Identity for Audit Logs
- **Decision**: Record `actor` as `anonymous` (string constant) until authentication feature provides user identity; include correlation ID and remote IP.
- **Rationale**: Keeps audit pipeline functional without blocking feature on auth; easily replaceable later.
- **Alternatives Considered**:
  - Reject updates without identity: Blocks current requirements.
  - Require custom header (X-User-Id): Introduces ad-hoc auth bypass; potential security risk.

### 3. DataSource Validation During Update
- **Decision**: Re-run full DataSource validations (path exists, file exists, not locked) only if DataSource is supplied AND simulation status is "Not Started". If status is already "In Progress" or "Completed" reject any attempt to change DataSource.
- **Rationale**: Preserves integrity pre-start; avoids unnecessary I/O when file not changing.
- **Alternatives Considered**:
  - Always revalidate: Wastes resources when DataSource unchanged.
  - Never revalidate: Risks stale/invalid path changes slipping through prior to start.

### 4. HTTP Method Choice
- **Decision**: Implement HTTP PATCH for partial updates with simple merge semantics (only provided fields applied). Do not support PUT in this feature.
- **Rationale**: Specification emphasizes partial updates; PATCH avoids clients sending entire resource representation.
- **Alternatives Considered**:
  - PUT full replace: Misaligned with partial behavior; higher accidental overwrite risk.
  - POST /{id}/update: Non-standard REST pattern.

### 5. Payload Format for PATCH
- **Decision**: Use a custom partial object (not JSON Patch RFC6902). Fields optional; absence means no change. Example: `{ "name": "New", "status": "In Progress" }`.
- **Rationale**: Simpler to implement and validate; less risk than operational JSON Patch instructions.
- **Alternatives Considered**:
  - JSON Patch: Powerful but overkill for small resource; more complex validation/security surface.
  - JSON Merge Patch: Similar outcome; explicit support not necessary; custom gives tighter control.

### 6. Status Transition Enforcement Location
- **Decision**: Domain service method `Simulation.ApplyUpdate(request)` evaluating transitions with current state; repository persists after validation.
- **Rationale**: Centralizes rules; improves testability; avoids scattering logic across controller.
- **Alternatives Considered**:
  - Controller-level validation: Violates Clean Architecture; harder to reuse.
  - EF Core interceptors: Indirect, less clear, more complex.

### 7. Concurrency Conflict Response Code
- **Decision**: Return HTTP 409 with body `{ "error": "Concurrency conflict", "currentVersion": "<ETag>" }`.
- **Rationale**: Aligns with common REST practice for conflicting modifications.
- **Alternatives Considered**:
  - 412 Precondition Failed: Also valid; selected 409 for consistency with existing 409 (file in use) semantics.

### 8. Audit Logging Data Set
- **Decision**: Log: simulationId, actor, changedFields[], oldValues (limited), newValues, correlationId, timestamp, ETag(before/after).
- **Rationale**: Sufficient traceability; avoids logging entire payload including potentially large DataSource path.
- **Alternatives Considered**:
  - Full entity snapshot: Larger logs; potential PII risk.
  - Only simulationId & actor: Insufficient forensic detail.

### 9. Transaction Boundary
- **Decision**: Single EF Core SaveChanges within implicit transaction; concurrency exception triggers rollback automatically.
- **Rationale**: EF handles rowversion concurrency elegantly; no multi-entity updates needed now.
- **Alternatives Considered**:
  - Manual transaction scope: Adds complexity with no benefit here.

### 10. Validation Strategy
- **Decision**: FluentValidation `UpdateSimulationValidator` with conditional rules (StartTime future only if supplied AND simulation not started; DataSource rules only if changing and pre-start).
- **Rationale**: Consolidated, testable; reuses patterns from creation.
- **Alternatives Considered**:
  - Manual controller checks: Less maintainable/reusable.

## Resolved Unknowns
- Concurrency token: ETag header with rowversion base64
- Actor identity: placeholder "anonymous"
- DataSource validation scope: conditional pre-start only

## No Remaining NEEDS CLARIFICATION Items
All previously marked unknowns are resolved; proceed to Phase 1 design.
