# PR: Implement GET Simulation Retrieval Endpoints (US1–US3)

Summary
- Adds three GET endpoints: list all, filter by start_time (inclusive), and get by id
- Deterministic ordering (StartTime ASC, Id ASC); UTC normalization
- Validation and standardized error responses (400, 404)
- Structured logging with correlation ID and durations

Changes
- API: `SimulationsController` GET actions; POST Location header standardized to `/api/simulations/{id}`
- Application: `ISimulationReadService`, `SimulationReadService`
- Infrastructure: `ISimulationReadRepository`, `SimulationReadRepository`
- Program: InMemory provider toggle for tests; Swagger XML comments
- Tests: Unit + Integration for US1–US3; test data files auto-created for POST scenarios
- Docs: spec, plan, research, data-model, quickstart updated; OpenAPI contract added

Testing
- All tests passing: 45/45
- Integration coverage for success/empty/invalid/404

Success Criteria
- SC-001/002 latency targets observed in logs during tests
- SC-003/004 validation behavior confirmed (400/404)
- SC-005 async-only DB access validated by code review
- SC-007 logging includes correlation ID and duration

Notes
- Pagination deferred (documented); StartTime index present
- Ready for review and merge
