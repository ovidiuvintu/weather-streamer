# Tasks: Retrieval Endpoints (GET Simulations)

## Dependency Graph (User Stories)
- US1 (P1): List all simulations (GET /simulations)
- US2 (P2): Filter simulations by start_time (GET /simulations/by-start-time) – depends on repository + DTO from US1
- US3 (P2): Get simulation by id (GET /simulations/{id}) – depends on repository + DTO from US1

Execution Order: US1 → (US2 || US3 in parallel)

Parallel Opportunities:
- US2 and US3 phases can run concurrently after foundational tasks
- Unit test tasks for service methods can run in parallel with controller endpoint tasks (distinct files)

MVP Scope: Complete US1 only (list endpoint + tests) provides operational visibility.

## Phase 1: Setup

- [X] T001 Ensure branch `002-get-simulations` is checked out
- [X] T002 Verify existing solution references all four layer projects in `WeatherStreamer.sln`
- [X] T003 [P] Confirm correlation ID middleware present (no code change) and note file path in plan (skip if existing) (`src/WeatherStreamer.Api/Middleware/CorrelationIdMiddleware.cs`)

## Phase 2: Foundational

- [X] T004 Create read service interface `src/WeatherStreamer.Application/Services/Simulations/ISimulationReadService.cs`
- [X] T005 Implement read service `src/WeatherStreamer.Application/Services/Simulations/SimulationReadService.cs`
- [X] T006 Create read repository interface `src/WeatherStreamer.Application/Repositories/ISimulationReadRepository.cs`
- [X] T007 Implement read repository `src/WeatherStreamer.Infrastructure/Repositories/SimulationReadRepository.cs`
- [X] T008 [P] Add StartTime index migration if not present `src/WeatherStreamer.Infrastructure/Migrations/<timestamp>_AddStartTimeIndex.cs` (Index already exists in initial migration)
- [X] T009 Add DTO `src/WeatherStreamer.Api/Models/SimulationDto.cs`
- [X] T010 [P] Register new interfaces in DI container `src/WeatherStreamer.Api/Program.cs`
 
 - [X] T004 Create read service interface `src/WeatherStreamer.Application/Services/Simulations/ISimulationReadService.cs`
 - [X] T005 Implement read service `src/WeatherStreamer.Application/Services/Simulations/SimulationReadService.cs`
 - [X] T006 Create read repository interface `src/WeatherStreamer.Application/Repositories/ISimulationReadRepository.cs`
 - [X] T007 Implement read repository `src/WeatherStreamer.Infrastructure/Repositories/SimulationReadRepository.cs`
 - [X] T009 Add DTO `src/WeatherStreamer.Api/Models/SimulationDto.cs`
 - [X] T010 [P] Register new interfaces in DI container `src/WeatherStreamer.Api/Program.cs`

## Phase 3: User Story US1 (P1) - List All Simulations

Goal: Provide GET /simulations returning ordered list of all simulations.
Independent Test: Seed 3 simulations → GET returns 3 ordered by StartTime then Id; empty DB returns [] with 200.

 
 [X] T015 [P] [US1] Add integration test empty list `tests/Integration/Simulations/GetAllSimulationsTests.cs`
 [X] T016 [P] [US1] Add integration test populated list ordering `tests/Integration/Simulations/GetAllSimulationsTests.cs`
 - [X] T011 [US1] Add service method GetAllAsync() in `SimulationReadService` (ordering + projection)
 - [X] T012 [US1] Add repository method GetAllAsync() in `SimulationReadRepository`
 - [X] T013 [US1] Implement controller action GET /simulations in `src/WeatherStreamer.Api/Controllers/SimulationsController.cs`
 - [X] T014 [P] [US1] Add unit tests for GetAllAsync ordering `tests/Unit/Application/SimulationReadServiceTests.cs`
 - [X] T015 [P] [US1] Add integration test empty list `tests/Integration/Simulations/GetAllSimulationsTests.cs`
 - [X] T016 [P] [US1] Add integration test populated list ordering `tests/Integration/Simulations/GetAllSimulationsTests.cs`
 - [X] T017 [US1] Add logging (count, duration) in controller/service

## Phase 4: User Story US2 (P2) - Filter By Start Time

Goal: Provide GET /simulations/by-start-time?start_time=ISO returning simulations with StartTime >= boundary.
Independent Test: Seed < earlier, boundary, later > times; only boundary & later returned. Invalid start_time → 400.

- [X] T018 [US2] Add service method GetFromStartTimeAsync(DateTime boundaryUtc) in `SimulationReadService`
- [X] T019 [US2] Add repository method GetFromStartTimeAsync(DateTime boundaryUtc) in `SimulationReadRepository`
- [X] T020 [US2] Implement controller action GET /simulations/by-start-time in `SimulationsController`
 - [X] T021 [P] [US2] Add unit tests for invalid date parsing path `tests/Unit/Application/SimulationReadServiceTests.cs` (N/A: date parsing handled in controller; covered by integration tests)
- [X] T022 [P] [US2] Add integration test valid filter matches subset `tests/Integration/Simulations/GetSimulationsByStartTimeTests.cs`
- [X] T023 [P] [US2] Add integration test no matches returns empty array `tests/Integration/Simulations/GetSimulationsByStartTimeTests.cs`
- [X] T024 [P] [US2] Add integration test invalid format returns 400 `tests/Integration/Simulations/GetSimulationsByStartTimeTests.cs`
- [X] T025 [US2] Add logging (boundary, count, duration)

## Phase 5: User Story US3 (P2) - Get Simulation By ID

Goal: Provide GET /simulations/{id} returning single simulation or 404; 400 for invalid id (<=0 or non-numeric).
Independent Test: Seed known id → 200 with record; unknown id → 404; id=0 → 400.

- [X] T026 [US3] Add service method GetByIdAsync(int id) in `SimulationReadService`
- [X] T027 [US3] Add repository method GetByIdAsync(int id) in `SimulationReadRepository`
- [X] T028 [US3] Implement controller action GET /simulations/{id} in `SimulationsController`
- [X] T029 [P] [US3] Add unit tests for GetByIdAsync not found `tests/Unit/Application/SimulationReadServiceTests.cs`
- [X] T030 [P] [US3] Add integration test found `tests/Integration/Simulations/GetSimulationByIdTests.cs`
- [X] T031 [P] [US3] Add integration test not found 404 `tests/Integration/Simulations/GetSimulationByIdTests.cs`
- [X] T032 [P] [US3] Add integration test invalid id 400 `tests/Integration/Simulations/GetSimulationByIdTests.cs`
- [X] T033 [US3] Add logging (id, duration, found?)

## Phase 6: Polish & Cross-Cutting

 - [X] T034 Add OpenAPI annotations / ensure Swagger reflects new endpoints `SimulationsController.cs`
 - [X] T035 [P] Add error response examples to Swagger (ProblemDetails or custom) `SimulationsController.cs`
 - [X] T036 Update `quickstart.md` if response shape changed `specs/002-get-simulations/quickstart.md`
 - [X] T037 Add performance smoke test script reference (manual) `specs/002-get-simulations/research.md`
 - [X] T038 Ensure constitution re-check note added to `plan.md`
 - [X] T039 Final review: ensure no blocking calls (.Result/Wait) across new code
 - [X] T040 Squash & prepare PR description referencing spec + success criteria

## Implementation Strategy
1. Complete foundational interfaces & DTO (Phases 1–2)
2. Deliver MVP (US1) → merge-ready partial feature
3. Parallelize US2 and US3 development & tests
4. Polish and documentation

## Independent Test Criteria Summary
- US1: 0 or N records returns 200, ordering enforced
- US2: Inclusive boundary, invalid format 400, empty subset 200 []
- US3: Found 200, not found 404, invalid id 400

## Task Counts
- Total Tasks: 40
- US1 Tasks: 7 (T011–T017)
- US2 Tasks: 8 (T018–T025)
- US3 Tasks: 8 (T026–T033)
- Setup + Foundational + Polish: 17

## Format Validation
All tasks follow: `- [ ] T### [P]? [USn]? Description with file path`.
