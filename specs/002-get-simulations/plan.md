# Implementation Plan: Retrieval Endpoints (GET Simulations)

**Branch**: `002-get-simulations` | **Date**: 2025-11-12 | **Spec**: `specs/002-get-simulations/spec.md`
**Input**: Feature specification from `/specs/002-get-simulations/spec.md`

## Summary

Implement three read-only HTTP GET endpoints for Simulation retrieval: list all, filter by inclusive `start_time`, and fetch by `id`. Queries are async EF Core read operations returning DTOs ordered by `StartTime ASC, Id ASC`. No pagination in this iteration (explicitly deferred); endpoints must meet latency targets (<500–600ms for ≤500 rows) and adhere to logging, validation, and clean architecture constraints. Future scalability concerns (pagination, max size) documented in research.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (runtime already adopted)  
**Primary Dependencies**: ASP.NET Core Minimal or Controller API (existing), Entity Framework Core (SQLite provider), FluentValidation, Swashbuckle (Swagger), Logging (Microsoft.Extensions.Logging)  
**Storage**: SQLite (file persistence) via EF Core DbContext (scoped per request)  
**Testing**: xUnit (assumed from prior tests), FluentAssertions, EF Core InMemory provider for integration tests  
**Target Platform**: Cross-platform .NET 8 (Windows dev; deploy Linux container friendly)  
**Project Type**: Web API backend (Clean Architecture layers: API, Application, Infrastructure, Domain)  
**Performance Goals**: Meet SC-001/SC-002 (<500ms / <600ms for ≤500 records). Avoid N+1 queries. Async-only DB access.  
**Constraints**: Async-first (no blocking), maintain ordering deterministically, JSON serialization stable, response size unbounded this iteration but highlight >10k risk.  
**Scale/Scope**: Current dataset expected small (<5k rows). Design ready for pagination addition without breaking public contract (add query params later). 

## Constitution Check

Preliminary compliance (all items planned):

- [x] **Clean Architecture**: Retrieval implemented via Application layer service + Repository (Infrastructure). Controllers thin.
- [x] **Database Integrity**: Using existing Simulation entity; no schema changes. Primary key enforced.
- [x] **Configuration**: Connection string already externalized (assumed). No new secrets introduced.
- [x] **Validation & Security**: Query/path params validated (start_time format, positive id). HTTPS + CORS unchanged.
- [x] **Error Handling**: Existing global exception middleware leveraged; standardized error DTO reused.
- [x] **Performance**: Simple indexed query (ensure index on StartTime—verify/migrate if absent). No caching needed now.
- [x] **Testing**: Unit tests for service logic + integration tests for endpoints (success, 400, 404, 500).
- [x] **Deployment**: No new persistence aspects; SQLite file remains persisted as configured.
- [x] **Documentation**: OpenAPI contracts added; Swagger renders automatically.
- [x] **Code Quality**: SOLID via service interfaces; no duplication.
- [x] **Async-First**: All EF calls use async (`ToListAsync`, `FirstOrDefaultAsync`).
- [x] **Layer Separation**: API → Application Service → Repository → DbContext.
- [x] **Logging**: Correlation ID already in pipeline; add structured log entries (duration, count, id).
- [x] **Database Standards**: EF Core LINQ queries; no raw SQL; potential StartTime index migration tracked.

**Violations Requiring Justification**: None.

Re-check (post-implementation): All new endpoints and changes adhere to the constitution. No blocking sync calls detected; logging and validation enforced; tests in place.

## Project Structure

### Documentation (this feature)

```text
specs/002-get-simulations/
├── plan.md          # Implementation plan
├── research.md      # Phase 0 research + risk analysis
├── data-model.md    # Simulation DTO + mapping notes
├── quickstart.md    # Usage examples
├── contracts/       # OpenAPI partial spec additions
└── tasks.md         # Generated in Phase 2
```

### Source Code (target additions)

```text
backend/
├── src/
│   ├── Domain/Simulation.cs                # Existing entity (reference only)
│   ├── Application/Simulations/ISimulationReadService.cs
│   ├── Application/Simulations/SimulationReadService.cs
│   ├── Infrastructure/Simulations/SimulationReadRepository.cs
│   ├── Infrastructure/Simulations/ISimulationReadRepository.cs
│   ├── API/Simulations/SimulationDto.cs
│   ├── API/Simulations/SimulationsController.cs (or minimal endpoints)
│   └── API/Errors/ErrorResponse.cs (reuse if exists)
└── tests/
    ├── Integration/Simulations/GetAllSimulationsTests.cs
    ├── Integration/Simulations/GetSimulationsByStartTimeTests.cs
    ├── Integration/Simulations/GetSimulationByIdTests.cs
    └── Unit/Application/SimulationReadServiceTests.cs
```

**Structure Decision**: Option 2 (Web application) focusing on backend only for this feature. Frontend unaffected.

## Complexity Tracking

No violations; section not required.
