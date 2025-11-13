# Data Model: Simulation Update

## Entities

### Simulation
- id: int (PK)
- name: string (max 70, required)
- startTimeUtc: datetime (UTC, required)
- fileName: string (required)
- status: enum { Not Started, In Progress, Completed } (required)
- rowVersion: byte[] (rowversion/timestamp, concurrency token)

## Constraints & Rules
- name: not null/empty, <= 70 chars
- startTimeUtc:
  - Must be strictly in the future when status == Not Started and when updated
  - Immutable once status ∈ { In Progress, Completed }
- fileName:
  - Windows-style path allowed chars: alphanumeric, space, hyphen, underscore, period, backslash
  - Max length 260; cannot start with digit
  - Path must exist, file must exist, file not locked (checked only if changed and status == Not Started)
  - Immutable once status ∈ { In Progress, Completed }
- status transitions (finite state machine):
  - Not Started → In Progress → Completed
  - Illegal: Not Started → Completed (skip), any backward transition, any change from Completed

## Derived/Behavioral
- ETag header = base64(rowVersion)
- Updates require If-Match header with prior ETag
- Audit log fields: simulationId, actor, changedFields, oldValues (limited), newValues, correlationId, timestamp, etagBefore, etagAfter

## Validation (FluentValidation)
- Conditional rules based on current persisted state and supplied fields
- Reject additional properties in PATCH payload

## Transactionality
- EF Core SaveChanges uses implicit transaction; concurrency exceptions roll back
