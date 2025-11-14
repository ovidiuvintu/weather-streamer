# research.md — Delete Simulation

Decision: Soft-delete (mark `IsDeleted` flag on `Simulation` row)

Rationale
- Preserves full audit trail in the primary table, making it straightforward to verify `AuditEntry` references and previous state without cross-table joins or archival scripts.
- Simplifies recovery and accidental-delete remediation: row can be un-deleted if necessary.
- Avoids complexity of maintaining a separate archive pipeline and reduces risk of losing referential integrity for `AuditEntry` records.

Alternatives Considered
- Hard delete: permanently remove row from primary table. Rejected because it complicates audit verification and recovery.
- Archive table: move deleted rows to `SimulationsArchive`. This keeps history separate but adds ETL complexity and potential referential integrity work for `AuditEntry` records.

Implementation Notes
- Add boolean `IsDeleted` column (or `DeletedAt` timestamp if time-of-deletion is needed).
- Public APIs (GET, list) should filter out soft-deleted rows by default; internal audit/reporting queries should be able to include them when required.
- Retain `RowVersion` concurrency token on the row; updates to `IsDeleted` must participate in concurrency checks (i.e., If-Match still required on DELETE).
- Persist `AuditEntry` with `Action: "Delete"`, `ChangesJson` representing the pre-delete snapshot, and `PrevEtag` the base64(RowVersion) before soft-delete.

Compatibility
- Tests using InMemory provider should seed `RowVersion` values where needed to avoid provider inconsistencies when asserting ETag-based behavior.

Next steps
- Add `IsDeleted` to data model and update repository queries to exclude soft-deleted rows.
- Add integration tests verifying soft-delete behavior and `AuditEntry` creation.
