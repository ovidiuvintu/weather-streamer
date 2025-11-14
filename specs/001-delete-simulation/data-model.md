# data-model.md — Delete Simulation

Entities

- Simulation
  - Id: integer (PK)
  - Name: string
  - StartTime: DateTime (nullable)
  - FileName: string
  - Status: enum [NotStarted, Running, Stopped, Completed]
  - RowVersion: byte[] (concurrency token)
  - IsDeleted: boolean (default: false)

  Validation rules
  - `Name` required, max length 200
  - `Status` must be a valid enum value
  - `IsDeleted` default false; set to true on successful DELETE with correct If-Match

- AuditEntry
  - Id: integer (PK)
  - SimulationId: integer (FK -> Simulation.Id)
  - Actor: string (who performed the action)
  - CorrelationId: string
  - TimestampUtc: DateTime
  - Action: string (e.g., "Delete")
  - ChangesJson: string (JSON snapshot of previous resource)
  - PrevEtag: string (base64(RowVersion) before change)

Constraints
- Simulation.Id PK
- AuditEntry.SimulationId FK -> Simulation.Id
- RowVersion configured as EF Core concurrency token (Timestamp attribute)
- `IsDeleted` included in any unique/index definitions where necessary (e.g., unique Name per non-deleted rows if applicable)

Queries
- Public GET / list queries: include `WHERE IsDeleted = 0` (or effect via global query filter in EF Core)
- Audit queries: join AuditEntry to Simulation by SimulationId (Simulation row retained even when IsDeleted=true)

State transitions & concurrency
- DELETE is a state transition that sets `IsDeleted = true`.
- The delete operation must check RowVersion via If-Match and atomically set `IsDeleted = true`, updating RowVersion in the same transaction.

Testing guidance
- Use in-memory DB for unit/integration tests but seed RowVersion values to simulate concurrency tokens when asserting ETag/If-Match behavior.
- Tests should assert that after DELETE the Simulation row exists with `IsDeleted = true` and an AuditEntry was persisted.
