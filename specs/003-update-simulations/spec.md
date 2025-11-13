# Feature Specification: Simulation Update

**Feature Branch**: `003-update-simulations`  
**Created**: 2025-11-12  
**Status**: Draft  
**Input**: User description: "the system MUST accept simulation update requests with JSON payload conforming to the specified schema with properties: Name (string), StartTime (ISO 8601 date-time string), DataSource (string) and Status"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Update an existing simulation (Priority: P1)

An operations user updates an existing simulation’s details (Name, StartTime, DataSource, Status) to correct or reschedule it before it starts.

**Why this priority**: Enables safe correction and rescheduling of simulations, preventing bad runs and ensuring timely execution.

**Independent Test**: Can be fully tested by submitting a valid update request for an existing simulation ID and verifying the returned confirmation and persisted changes.

**Acceptance Scenarios**:

1. **Given** an existing simulation with ID 42 and status "Not Started", **When** the user submits an update with a new Name, a future StartTime, a valid DataSource, and Status "Not Started", **Then** the system returns a success response and persists all new values.
2. **Given** an existing simulation with ID 42 and status "Not Started", **When** the user updates only the Status to "In Progress" in accordance with allowed transitions, **Then** the system returns success and the status changes to "In Progress".

---

### User Story 2 - Enforce valid status transitions (Priority: P2)

An operations user changes the simulation Status following business rules so that execution lifecycle remains consistent and auditable.

**Why this priority**: Prevents illegal state changes that could compromise scheduling, reporting, or downstream processes.

**Independent Test**: Can be fully tested by attempting various status changes and verifying allowed transitions succeed and invalid transitions are rejected.

**Acceptance Scenarios**:

1. **Given** a simulation in "Not Started", **When** the user sets Status to "In Progress", **Then** the system accepts the update.
2. **Given** a simulation in "In Progress", **When** the user sets Status to "Completed", **Then** the system accepts the update.
3. **Given** a simulation in "Completed", **When** the user attempts to set Status to any other value, **Then** the system rejects the update with a clear error.

---

### User Story 3 - Validate fields on update (Priority: P3)

An operations user attempts to update with invalid values; the system rejects the request with clear messages without changing existing data.

**Why this priority**: Protects data integrity and usability by preventing invalid or unsafe updates.

**Independent Test**: Can be fully tested by submitting updates with invalid Name length, non-existent DataSource, or StartTime not strictly in the future (when applicable) and verifying rejection and audit logging.

**Acceptance Scenarios**:

1. **Given** an update where Name exceeds the maximum allowed length, **When** the request is submitted, **Then** the system returns a validation error and does not persist changes.
2. **Given** an update where DataSource path does not exist or file is missing, **When** the request is submitted, **Then** the system returns a clear error and does not persist changes.
3. **Given** an update where StartTime is not strictly in the future for a "Not Started" simulation, **When** the request is submitted, **Then** the system returns a clear error and does not persist changes.

---

### Edge Cases

- Updating a non-existent simulation ID → return a not found error without side effects
- Attempting to change DataSource or StartTime after simulation has started → reject with clear error
- Attempting to update with no changes (identical values) → succeed with idempotent behavior and indicate no changes applied
- Concurrent updates on the same simulation → use optimistic concurrency with versioning; conflicting updates are rejected and must be retried by the client with the latest version
- Partial vs full updates (e.g., supplying only Status) → partial updates are supported; unspecified fields retain their current values

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow updating an existing simulation by identifier via an API request that accepts a JSON payload containing: Name (string), StartTime (ISO 8601 date-time string), DataSource (string), and Status (enum: "Not Started", "In Progress", "Completed").
- **FR-002**: System MUST validate the payload on update using the same business rules as creation for Name, StartTime, and DataSource, except where explicitly overridden for update semantics.
- **FR-003**: System MUST enforce that StartTime can only be set to a future instant for simulations that are not yet started; StartTime MUST NOT be modified once the simulation is "In Progress" or "Completed".
- **FR-004**: System MUST enforce allowed status transitions: "Not Started" → "In Progress" → "Completed"; transitions that skip forward (e.g., "Not Started" → "Completed") or move backwards are rejected with a clear error message.
- **FR-005**: System MUST reject updates that attempt to change DataSource after the simulation has started (status is "In Progress" or "Completed").
- **FR-006**: System MUST support partial updates; fields omitted from the request MUST retain their existing values.
- **FR-007**: System MUST return clear error messages for validation failures and MUST NOT persist any partial changes when an update fails.
- **FR-008**: System MUST return a not found error when the specified simulation identifier does not exist.
- **FR-009**: System MUST use optimistic concurrency with versioning; when a concurrency conflict is detected, the update MUST be rejected with a conflict response and the client informed to retry using the latest version.
- **FR-010**: System MUST record audit information for updates (who/when/what changed) sufficient for operational traceability.

### Assumptions

- Authorized operations users perform updates; authentication/authorization is governed elsewhere.
- Status values remain: "Not Started", "In Progress", "Completed"; no additional statuses are introduced by this feature.
- Performance expectations for update mirror creation operations (see Success Criteria).
- Audit logging infrastructure exists to capture who/when/what for updates.

### Key Entities *(include if feature involves data)*

- **Simulation**: Existing record representing a weather data simulation. Updatable fields: Name, StartTime (subject to rules), DataSource (subject to rules), Status (subject to allowed transitions). Immutable identifiers are not changed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of successful simulation updates complete within 2 seconds under normal operating conditions.
- **SC-002**: 100% of invalid updates are rejected with specific, user-understandable messages and no data mutation.
- **SC-003**: 100% enforcement of allowed status transitions; no illegal transitions are persisted.
- **SC-004**: 100% of updates against non-existent identifiers return a not found response without side effects.
- **SC-005**: When concurrency conflicts occur (as per selected policy), 100% are handled according to the defined rules and are observable via audit logs.
