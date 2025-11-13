# Tasks: 003-update-simulations

Input: Design documents from `/specs/003-update-simulations/`
Prerequisites: plan.md (required), spec.md, research.md, data-model.md, contracts/

Format: `[ID] [P?] [Story] Description`
- [P]: Can run in parallel (different files, no deps)
- [Story]: US1, US2, US3 per spec

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Verify solution builds and tests run locally (no code changes)
- [ ] T002 Ensure .gitignore includes obj/, bin/, logs/ for .NET projects

---

## Phase 2: Foundational (Blocking Prerequisites)

- [x] T010 Add concurrency token to entity: add `RowVersion` byte[] with `[Timestamp]` in `src/WeatherStreamer.Domain/Entities/Simulation.cs`; configure as concurrency token in `src/WeatherStreamer.Infrastructure/Data/Configurations/SimulationConfiguration.cs`
- [ ] T011 Create EF migration `AddSimulationRowVersion` and update local dev DB
- [x] T012 [P] Expose ETag on read responses: add ETag header to GET `/api/simulations/{id}` using base64(rowversion)
- [x] T013 [P] Wire rowversion mapping in repository/service DTOs so reads include ETag value (if applicable)

Checkpoint: Concurrency token and ETag available; proceed to user stories

---

## Phase 3: User Story 1 - Update an existing simulation (P1) 🎯 MVP

Goal: Partial update of Name, StartTime, DataSource, Status with optimistic concurrency
Independent Test: Submit PATCH with If-Match ETag and verify persisted changes

### Tests (write first, ensure FAIL)
- [x] T020 [US1] Integration: PATCH updates Name only; expect 200 and new ETag in `tests/WeatherStreamer.IntegrationTests/Controllers/UpdateSimulationTests.cs`
- [x] T021 [US1] Integration: Concurrency conflict with stale If-Match returns 409 with currentVersion
- [x] T022 [US1] Unit: Validator allows partial payload; enforces conditional rules in `tests/WeatherStreamer.UnitTests/Validators/UpdateSimulationValidatorTests.cs`

### Implementation
 - [x] T025 [US1] Application: Add `UpdateSimulationCommand` and Handler in `src/WeatherStreamer.Application/Services/Simulations/Update/`
 - [x] T026 [US1] Application: Add `UpdateSimulationValidator` with conditional rules
 - [x] T027 [US1] Domain: Add method to apply updates and enforce immutability gates for StartTime/DataSource when started
 - [x] T028 [US1] API: Add PATCH `/api/simulations/{id}` in `SimulationsController.cs` with If-Match header handling and ETag response
 - [x] T029 [US1] Infrastructure: Map rowversion column to entity; ensure EF concurrency exceptions mapped to 409
 - [x] T030 [US1] Logging/Audit: Log actor="anonymous", changed fields, etag before/after

Checkpoint: PATCH endpoint functional with concurrency and basic rules

---

## Phase 4: User Story 2 - Enforce valid status transitions (P2)

Goal: Only allow NotStarted → InProgress → Completed
Independent Test: Illegal transitions rejected with clear error

### Tests (write first)
- [ ] T040 [US2] Unit: Transition matrix tests in domain/service layer
- [ ] T041 [US2] Integration: PATCH NotStarted→Completed returns 409/400 with message

### Implementation
- [ ] T045 [US2] Domain/Application: Implement transition checks centrally (service or domain method)
- [ ] T046 [US2] API: Map illegal transitions to consistent error response

Checkpoint: Status transitions enforced end-to-end

---

## Phase 5: User Story 3 - Validate fields on update (P3)

Goal: Reject invalid Name, DataSource path, StartTime not future (pre-start)
Independent Test: Invalid updates rejected; no data mutation

### Tests (write first)
- [ ] T050 [US3] Unit: Name length >70 rejected
- [ ] T051 [US3] Unit: DataSource invalid path chars rejected
- [ ] T052 [US3] Integration: DataSource change when file missing returns 404

### Implementation
- [ ] T055 [US3] Validation: Extend `UpdateSimulationValidator` with conditional DataSource rules (only if changed and status NotStarted)
- [ ] T056 [US3] File checks: Reuse existing file validation services; add lock detection if available

Checkpoint: Validation parity achieved

---

## Phase 6: Polish & Docs

- [ ] T060 Swagger: Document PATCH with If-Match and ETag; align with `contracts/openapi.update-simulations.yaml`
- [ ] T061 Docs: Update quickstart.md validation steps executed
- [ ] T062 Logging: Verify structured logs include correlationId and audit fields
- [ ] T063 Cleanup: Code comments, XML docs for public APIs

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → User stories in order (P1 → P2 → P3) → Polish
- Tests are written before implementation tasks within each story
- Tasks marked [P] can run in parallel when files don’t conflict
