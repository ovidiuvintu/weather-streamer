---
description: "Task list for Simulation Control Module implementation"
---

# Tasks: Weather Simulation Control Module

**Feature**: 001-simulation-control  
**Input**: Design documents from `/specs/001-simulation-control/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/post-simulations.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution and project structure with 4 Clean Architecture layers (API, Application, Infrastructure, Domain) plus 2 test projects
- [X] T002 Add project references: API→Application+Infrastructure, Application→Domain, Infrastructure→Domain+Application
- [X] T003 Install NuGet packages for API layer: Microsoft.EntityFrameworkCore.Design 8.0.0, Serilog.AspNetCore 8.0.0, Serilog.Sinks.Console 5.0.1, Serilog.Sinks.File 5.0.0, AspNetCoreRateLimit 5.0.0, Swashbuckle.AspNetCore 6.5.0
- [X] T004 [P] Install NuGet packages for Application layer: FluentValidation 11.8.1, FluentValidation.DependencyInjectionExtensions 11.8.1
- [X] T005 [P] Install NuGet packages for Infrastructure layer: Microsoft.EntityFrameworkCore 8.0.0, Microsoft.EntityFrameworkCore.Sqlite 8.0.0, Microsoft.EntityFrameworkCore.InMemory 8.0.0
- [X] T006 [P] Install NuGet packages for UnitTests: Moq 4.20.70, FluentAssertions 6.12.0, Microsoft.EntityFrameworkCore.InMemory 8.0.0
- [X] T007 [P] Install NuGet packages for IntegrationTests: Microsoft.AspNetCore.Mvc.Testing 8.0.0, FluentAssertions 6.12.0, Microsoft.EntityFrameworkCore.InMemory 8.0.0
- [X] T008 Create appsettings.json with production configuration in src/WeatherStreamer.Api/appsettings.json
- [X] T009 Create appsettings.Development.json with in-memory database and rate limiting config in src/WeatherStreamer.Api/appsettings.Development.json

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T010 Create SimulationStatus enum with NotStarted, InProgress, Completed values in src/WeatherStreamer.Domain/Enums/SimulationStatus.cs
- [X] T011 Create Simulation entity with Id, Name, StartTime, FileName, Status properties in src/WeatherStreamer.Domain/Entities/Simulation.cs
- [X] T012 Create SimulationConfiguration implementing IEntityTypeConfiguration<Simulation> with explicit mappings, indexes, and constraints in src/WeatherStreamer.Infrastructure/Data/Configurations/SimulationConfiguration.cs
- [X] T013 Create WeatherStreamerDbContext with DbSet<Simulation> and OnModelCreating configuration in src/WeatherStreamer.Infrastructure/Data/WeatherStreamerDbContext.cs
- [X] T014 Create ISimulationRepository interface with CreateAsync and IsFileInUseAsync methods in src/WeatherStreamer.Infrastructure/Repositories/ISimulationRepository.cs
- [X] T015 Create SimulationRepository implementing ISimulationRepository with EF Core operations in src/WeatherStreamer.Infrastructure/Repositories/SimulationRepository.cs
- [X] T016 [P] Create IFileValidationService interface with ValidateFileAsync method in src/WeatherStreamer.Application/Services/IFileValidationService.cs
- [X] T017 [P] Create FileValidationService implementing file system checks (existence, lock detection) in src/WeatherStreamer.Infrastructure/Services/FileValidationService.cs
- [X] T018 [P] Create CreateSimulationRequest DTO with Name, StartTime, DataSource properties in src/WeatherStreamer.Api/Models/CreateSimulationRequest.cs
- [X] T019 [P] Create CreateSimulationResponse DTO with Id, Name, StartTimeUtc, DataSource, Status properties in src/WeatherStreamer.Api/Models/CreateSimulationResponse.cs
- [X] T020 [P] Create ErrorResponse DTO with CorrelationId, Timestamp, StatusCode, Error, Details properties in src/WeatherStreamer.Api/Models/ErrorResponse.cs
- [X] T021 Create CorrelationIdMiddleware to generate and track correlation IDs per request in src/WeatherStreamer.Api/Middleware/CorrelationIdMiddleware.cs
- [X] T022 Create GlobalExceptionMiddleware for centralized error handling with consistent ErrorResponse format in src/WeatherStreamer.Api/Middleware/GlobalExceptionMiddleware.cs
- [X] T023 Configure Serilog in Program.cs with structured logging, correlation ID enrichment, and JSON output
- [X] T024 Configure rate limiting in Program.cs with AspNetCoreRateLimit (100 requests/minute per IP)
- [ ] T025 Configure Swagger/OpenAPI in Program.cs with Swashbuckle
- [X] T026 Register services in Program.cs: DbContext, repositories, services, validators
- [X] T027 Configure middleware pipeline in Program.cs: correlation ID → rate limiting → global exception → routing → endpoints
- [X] T028 Create initial EF Core migration with 'dotnet ef migrations add InitialCreate' from Infrastructure project
- [X] T029 Apply migration to create database schema with 'dotnet ef database update'

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Create Valid Weather Simulation (Priority: P1) 🎯 MVP

**Goal**: Enable operators to create valid simulations with comprehensive validation, returning HTTP 201 with simulation ID

**Independent Test**: Submit valid JSON payload with proper Name, StartTime, and DataSource referencing existing CSV file; verify HTTP 201 response with auto-generated ID and Location header

### Implementation for User Story 1

- [X] T030 [P] [US1] Create ISimulationService interface with CreateSimulationAsync method in src/WeatherStreamer.Application/Services/ISimulationService.cs
- [X] T031 [US1] Create SimulationService implementing business logic orchestration: validation → file checks → database creation in src/WeatherStreamer.Application/Services/SimulationService.cs
- [X] T032 [US1] Create CreateSimulationRequestValidator with FluentValidation rules: required fields, Name max 70 chars, StartTime ISO 8601 format in src/WeatherStreamer.Application/Validators/CreateSimulationRequestValidator.cs
- [X] T033 [US1] Create SimulationsController with POST endpoint returning 201 Created with Location header in src/WeatherStreamer.Api/Controllers/SimulationsController.cs
- [X] T034 [US1] Implement UTC conversion for StartTime in SimulationService
- [X] T035 [US1] Implement auto-generation of simulation ID via EF Core identity in database operation
- [X] T036 [US1] Add structured logging for successful simulation creation with correlation ID in SimulationService

### Tests for User Story 1

- [X] T037 [P] [US1] Unit test: CreateSimulationAsync_WithValidRequest_ReturnsSimulationId in tests/WeatherStreamer.UnitTests/Services/SimulationServiceTests.cs
- [X] T038 [P] [US1] Unit test: Validate_WithValidRequest_PassesValidation in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs
- [X] T039 [P] [US1] Integration test: POST_Simulations_WithValidRequest_Returns201Created in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [X] T040 [P] [US1] Integration test: POST_Simulations_VerifyLocationHeaderIn201Response in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [X] T041 [P] [US1] Integration test: POST_Simulations_VerifyCorrelationIdInResponse in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs

**Checkpoint**: At this point, User Story 1 should be fully functional - operators can create valid simulations and receive confirmation with ID

---

## Phase 4: User Story 2 - Handle Invalid Input Gracefully (Priority: P2)

**Goal**: Detect and reject malformed/invalid simulation data with clear error messages and appropriate HTTP status codes

**Independent Test**: Submit various invalid payloads (malformed JSON, missing fields, invalid data types) and verify appropriate 400 Bad Request responses with field-specific error details

### Implementation for User Story 2

- [X] T042 [US2] Add validation rules in CreateSimulationRequestValidator: Name not empty, StartTime valid ISO 8601, DataSource not empty
- [X] T043 [US2] Add JSON deserialization error handling in GlobalExceptionMiddleware returning HTTP 400
- [X] T044 [US2] Add Content-Type validation in SimulationsController (must be application/json) returning HTTP 400
- [X] T045 [US2] Add additional properties validation to reject JSON fields beyond schema returning HTTP 400 with field names listed
- [X] T046 [US2] Add detailed error logging in GlobalExceptionMiddleware with validation rule failures and correlation IDs
- [X] T047 [US2] Add Status field rejection validation in CreateSimulationRequestValidator returning HTTP 400 if Status provided in request

### Tests for User Story 2

- [X] T048 [P] [US2] Unit test: Validate_WithMissingName_ReturnsValidationError in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs
- [X] T049 [P] [US2] Unit test: Validate_WithInvalidStartTime_ReturnsValidationError in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs
- [X] T050 [P] [US2] Integration test: POST_Simulations_WithMissingName_Returns400BadRequest in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [X] T051 [P] [US2] Integration test: POST_Simulations_WithInvalidJSON_Returns400BadRequest in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [X] T052 [P] [US2] Integration test: POST_Simulations_WithAdditionalProperties_Returns400BadRequest in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [X] T053 [P] [US2] Integration test: POST_Simulations_WithStatusField_Returns400BadRequest in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs

**Checkpoint**: At this point, User Stories 1 AND 2 should both work - valid requests succeed with 201, invalid requests fail with 400 and clear error messages

---

## Phase 5: User Story 3 - Validate Data Source Files (Priority: P2)

**Goal**: Ensure CSV data source files are valid, accessible, and not concurrently in use by in-progress simulations

**Independent Test**: Submit requests with various file scenarios (non-existent paths, missing files, invalid filenames, duplicate concurrent usage) and verify appropriate error responses (400, 404, 409, 423)

### Implementation for User Story 3

- [ ] T054 [US3] Add DataSource validation rules in CreateSimulationRequestValidator: max 260 chars, no numeric prefix, special character restrictions (alphanumeric, spaces, hyphens, underscores, periods, backslashes only)
- [ ] T055 [US3] Implement path existence validation in FileValidationService returning HTTP 400 if path not found
- [ ] T056 [US3] Implement file existence validation in FileValidationService returning HTTP 404 if file not found
- [x] T057 [US3] Implement file lock detection in FileValidationService catching IOException and returning HTTP 423
- [x] T058 [US3] Implement concurrent file usage check in SimulationRepository querying for FileName + Status=InProgress
- [x] T059 [US3] Add concurrent file conflict handling in SimulationService returning HTTP 409 when file in use
- [x] T060 [US3] Add structured logging for file validation operations (path checked, file checked, lock detected, concurrent usage detected)

### Tests for User Story 3

- [x] T061 [P] [US3] Unit test: Validate_WithFileNameOver260Chars_ReturnsValidationError in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs (covered by existing test Validate_WithDataSourceTooLong_ReturnsValidationError)
- [x] T062 [P] [US3] Unit test: Validate_WithFileNameStartingWithDigit_ReturnsValidationError in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs (covered by existing test Validate_WithNumericFilenamePrefix_ReturnsValidationError)
- [x] T063 [P] [US3] Unit test: Validate_WithInvalidSpecialChars_ReturnsValidationError in tests/WeatherStreamer.UnitTests/Validators/CreateSimulationRequestValidatorTests.cs (covered by existing test Validate_WithInvalidCharactersInDataSource_ReturnsValidationError)
- [x] T064 [P] [US3] Unit test: ValidateFileAsync_WithNonExistentPath_ThrowsException in tests/WeatherStreamer.UnitTests/Services/FileValidationServiceTests.cs
- [x] T065 [P] [US3] Unit test: ValidateFileAsync_WithNonExistentFile_ThrowsException in tests/WeatherStreamer.UnitTests/Services/FileValidationServiceTests.cs
- [x] T066 [P] [US3] Unit test: ValidateFileAsync_WithLockedFile_ThrowsLockedException in tests/WeatherStreamer.UnitTests/Services/FileValidationServiceTests.cs
- [x] T067 [P] [US3] Unit test: CreateSimulationAsync_WithConcurrentFileUsage_ThrowsConflictException in tests/WeatherStreamer.UnitTests/Services/SimulationServiceTests.cs (covered by existing test CreateSimulationAsync_FileInUse_ThrowsInvalidOperationException)
- [x] T068 [P] [US3] Integration test: POST_Simulations_WithNonExistentFile_Returns404NotFound in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [x] T069 [P] [US3] Integration test: POST_Simulations_WithConcurrentFileUsage_Returns409Conflict in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs

**Checkpoint**: All user stories 1-3 should now be independently functional - comprehensive file validation prevents invalid data sources

---

## Phase 6: User Story 4 - Handle Database Errors (Priority: P3)

**Goal**: Gracefully handle database connectivity issues and storage failures with rollback and detailed error logging

**Independent Test**: Simulate database connectivity issues or constraint violations and verify error handling, rollback behavior, HTTP 500 responses, and logging

### Implementation for User Story 4

- [x] T070 [US4] Add database connection error handling in SimulationRepository catching DbUpdateException
- [x] T071 [US4] Add database constraint violation handling in SimulationRepository with transaction rollback
- [x] T072 [US4] Add transaction failure handling in GlobalExceptionMiddleware returning HTTP 500 for database errors
- [x] T073 [US4] Add detailed database error logging in SimulationService with exception details, stack traces, and correlation IDs
- [x] T074 [US4] Implement implicit transaction via SaveChangesAsync ensuring atomic operations with automatic rollback

### Tests for User Story 4

- [x] T075 [P] [US4] Unit test: CreateSimulationAsync_WithDatabaseError_ThrowsAndLogsException in tests/WeatherStreamer.UnitTests/Services/SimulationServiceTests.cs
- [x] T076 [P] [US4] Integration test: POST_Simulations_WithDatabaseError_Returns500InternalServerError in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [x] T077 [P] [US4] Integration test: POST_Simulations_VerifyTransactionRollbackOnError in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs

**Checkpoint**: All user stories should now be independently functional with complete error handling

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and ensure constitutional compliance

- [x] T078 [P] Add health check endpoint at /health with database connectivity check in src/WeatherStreamer.Api/Program.cs
- [ ] T079 [P] Create TestWebApplicationFactory for integration tests in tests/WeatherStreamer.IntegrationTests/Infrastructure/TestWebApplicationFactory.cs
- [ ] T080 [P] Create DatabaseFixture for test data seeding in tests/WeatherStreamer.IntegrationTests/Fixtures/DatabaseFixture.cs
- [ ] T081 [P] Integration test: POST_Simulations_RateLimitExceeded_Returns429TooManyRequests (send 101 requests) in tests/WeatherStreamer.IntegrationTests/Controllers/SimulationsControllerTests.cs
- [x] T082 Verify all async operations use async/await (no .Result, .Wait()) per Constitution Principle XI (verified - no blocking calls found)
- [x] T083 Verify layer separation enforced: Controller→Service→Repository→DbContext per Constitution Principle XII (verified - Clean Architecture maintained)
- [x] T084 Verify EF Core connection pooling configured in connection string per Constitution Principle VI (verified - Pooling=True with Min/Max pool size)
- [x] T085 Create test CSV file at C:\test-data\sample.csv for manual testing
- [ ] T086 Run manual testing scenarios from quickstart.md (Swagger UI, curl, Postman)
- [ ] T087 Verify OpenAPI/Swagger documentation displays all request/response schemas correctly at https://localhost:5001/swagger (blocked - Swagger removed due to .NET 10 RC compatibility)
- [x] T088 Run all unit tests and verify 100% pass rate (24/24 tests passing)
- [ ] T089 Run all integration tests and verify 100% pass rate
- [x] T090 Verify structured logging captures correlation IDs in all log events (verified - LogContext.PushProperty in CorrelationIdMiddleware)
- [x] T091 Update README.md with setup instructions, API usage examples, and deployment guidance
- [ ] T092 Run quickstart.md validation checklist (19 functional requirements + 14 constitutional compliance items)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion - BLOCKS all user stories
- **User Stories (Phases 3-6)**: All depend on Foundational (Phase 2) completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order: US1 (P1) → US2 (P2) → US3 (P2) → US4 (P3)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - **MVP candidate**
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Extends US1 validation but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Adds file validation to US1 but independently testable
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Adds error handling to US1 but independently testable

### Within Each User Story

- Implementation tasks before tests (unless doing TDD)
- Validators before services
- Services before controllers
- Core logic before logging
- Story complete before moving to next priority

### Parallel Opportunities

#### Phase 1 (Setup)
- T004, T005, T006, T007 (NuGet packages for different layers) can run in parallel

#### Phase 2 (Foundational)
- T016 and T017 (file validation service interface + implementation) can run in parallel
- T018, T019, T020 (DTOs) can run in parallel

#### User Story 1
- T030 can run while T031-T032 are designed
- T037, T038, T039, T040, T041 (all tests) can run in parallel

#### User Story 2
- T048, T049, T050, T051, T052, T053 (all tests) can run in parallel

#### User Story 3
- T061, T062, T063, T064, T065, T066, T067, T068, T069 (all tests) can run in parallel

#### User Story 4
- T075, T076, T077 (all tests) can run in parallel

#### Phase 7 (Polish)
- T078, T079, T080, T081 (health check, test infrastructure) can run in parallel

#### Multiple User Stories
- Once Foundational (Phase 2) completes, different team members can work on US1, US2, US3, US4 in parallel

---

## Parallel Example: User Story 1

```bash
# Launch service interface and implementation design concurrently:
Task T030: "Create ISimulationService interface"
Task T031: "Create SimulationService implementation" (after T030)

# Launch all unit/integration tests for User Story 1 together:
Task T037: "Unit test: CreateSimulationAsync_WithValidRequest_ReturnsSimulationId"
Task T038: "Unit test: Validate_WithValidRequest_PassesValidation"
Task T039: "Integration test: POST_Simulations_WithValidRequest_Returns201Created"
Task T040: "Integration test: POST_Simulations_VerifyLocationHeaderIn201Response"
Task T041: "Integration test: POST_Simulations_VerifyCorrelationIdInResponse"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only) - Recommended

1. Complete Phase 1: Setup (T001-T009)
2. Complete Phase 2: Foundational (T010-T029) - **CRITICAL - blocks all stories**
3. Complete Phase 3: User Story 1 (T030-T041)
4. **STOP and VALIDATE**: Test User Story 1 independently with valid simulation creation
5. Deploy/demo if ready - **Operators can now create valid simulations!**

### Incremental Delivery

1. Complete Setup + Foundational (T001-T029) → Foundation ready
2. Add User Story 1 (T030-T041) → Test independently → Deploy/Demo (**MVP - operators can create simulations!**)
3. Add User Story 2 (T042-T053) → Test independently → Deploy/Demo (**Better error messages for invalid input**)
4. Add User Story 3 (T054-T069) → Test independently → Deploy/Demo (**File validation prevents bad data sources**)
5. Add User Story 4 (T070-T077) → Test independently → Deploy/Demo (**Robust database error handling**)
6. Add Polish (T078-T092) → Final validation → Production deployment
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup (Phase 1) + Foundational (Phase 2) together (T001-T029)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (T030-T041) - **MVP work**
   - **Developer B**: User Story 2 (T042-T053) - Input validation
   - **Developer C**: User Story 3 (T054-T069) - File validation
   - **Developer D**: User Story 4 (T070-T077) - Database error handling
3. Stories complete and integrate independently
4. Team reconvenes for Polish (Phase 7) together (T078-T092)

---

## Task Summary

**Total Tasks**: 92  
**Setup**: 9 tasks  
**Foundational**: 20 tasks (BLOCKING for all user stories)  
**User Story 1 (P1 - MVP)**: 12 tasks (6 implementation + 5 tests + 1 checkpoint)  
**User Story 2 (P2)**: 12 tasks (6 implementation + 6 tests + 1 checkpoint)  
**User Story 3 (P2)**: 16 tasks (7 implementation + 9 tests + 1 checkpoint)  
**User Story 4 (P3)**: 8 tasks (5 implementation + 3 tests + 1 checkpoint)  
**Polish**: 15 tasks  

**Parallel Opportunities Identified**: 25+ tasks can run in parallel (marked with [P])

**Independent Test Criteria**:
- **US1**: Submit valid JSON with existing CSV file → Verify HTTP 201 with simulation ID
- **US2**: Submit invalid payloads (malformed JSON, missing fields) → Verify HTTP 400 with field errors
- **US3**: Submit requests with invalid file paths → Verify HTTP 400/404/409/423 responses
- **US4**: Simulate database errors → Verify HTTP 500 with rollback and logging

**Suggested MVP Scope**: User Story 1 only (T001-T041) - Operators can create valid simulations and receive confirmation

---

## Notes

- **[P]** tasks = different files, no dependencies within same phase
- **[Story]** label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Tests verify all acceptance scenarios from spec.md
- All tasks follow Constitution principles (Clean Architecture, async/await, structured logging, EF Core best practices)
- quickstart.md provides detailed implementation guidance for each task
