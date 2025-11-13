# Implementation Plan: Update Simulations (003)

**Branch**: `003-update-simulations` | **Date**: 2025-11-12 | **Spec**: /specs/003-update-simulations/spec.md
**Input**: Feature specification from `/specs/003-update-simulations/spec.md`

## Summary

Add partial update capability for Simulation resources with optimistic concurrency and strict status-transition rules. Provide a PATCH endpoint at `/api/simulations/{id}` that accepts any subset of fields: Name, StartTime, DataSource, Status, plus a required `version` token to enforce optimistic concurrency. Disallow StartTime/DataSource changes once a simulation has started; enforce linear Status transitions Not Started → In Progress → Completed; reject illegal transitions. Use FluentValidation for payload rules; persist via EF Core with a concurrency token; log audit events for who/when/what changed.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (ASP.NET Core Web API)
**Primary Dependencies**: ASP.NET Core MVC, EF Core 8, FluentValidation, Swashbuckle (Swagger)
**Storage**: SQLite in prod; InMemory provider for tests; EF Core entity with row version (ulong/byte[]) for concurrency
**Testing**: xUnit or NUnit with WebApplicationFactory for integration; unit tests for validators and services
**Target Platform**: Windows and cross-platform; Windows paths supported for DataSource validation
**Project Type**: Web backend (Clean Architecture: Api, Application, Infrastructure, Domain)
**Performance Goals**: 95% of updates < 2s (SC-001)
**Constraints**: Async-only I/O; no blocking calls; maintain transactionality on update
**Scale/Scope**: Matches existing GET/POST features; single resource type (Simulation)

Unknowns marked for research:
- Concurrency token shape returned by reads and used by updates (ETag vs body field) → RESOLVED: Use ETag/If-Match with rowversion
- Actor identity for audit logs (no auth in scope) → RESOLVED: use "anonymous" placeholder until auth lands
- Exact validation parity with creation for DataSource existence/locked check during update when status is Not Started → RESOLVED: revalidate only if DataSource changes and status is Not Started

## Constitution Check

Verify compliance with Weather Streamer Constitution v1.0.0 (post-design re-evaluation):

- [x] Clean Architecture (API → Application → Infrastructure → DB; DTOs, services, repos)
- [x] Database Integrity (rowVersion adds concurrency safety; constraints preserved)
- [x] Configuration (no hardcoded config; reuse existing)
- [x] Validation & Security (FluentValidation rules defined; no new security gaps)
- [x] Error Handling (reuse global middleware; consistent codes 400/404/409/423)
- [x] Performance (simple EF operations; meets <2s goal)
- [x] Testing (unit + integration test plan in quickstart)
- [x] Deployment (one additive migration; backwards compatible)
- [x] Documentation (OpenAPI contract added; plan + quickstart)
- [x] Code Quality (SOLID applied in handler/service separation)
- [x] Async-First (all planned EF/file operations async)
- [x] Layer Separation (no direct controller→repository calls planned)
- [x] Logging (audit + structured logs with correlation ID)
- [x] Database Standards (explicit configuration + migration + concurrency token)

Violations Requiring Justification: None anticipated

## Project Structure

### Documentation (this feature)

```text
specs/003-update-simulations/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── openapi.update-simulations.yaml
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── WeatherStreamer.Api/Controllers/SimulationsController.cs
│   ├── WeatherStreamer.Application/Simulations/Commands/UpdateSimulation/
│   │   ├── UpdateSimulationCommand.cs
│   │   ├── UpdateSimulationHandler.cs
│   │   └── UpdateSimulationValidator.cs
│   ├── WeatherStreamer.Domain/Entities/Simulation.cs
│   └── WeatherStreamer.Infrastructure/Persistence/
│       ├── AppDbContext.cs
│       └── Configurations/SimulationConfiguration.cs
└── tests/
    ├── WeatherStreamer.Api.IntegrationTests/Simulations/UpdateSimulationTests.cs
    └── WeatherStreamer.Application.UnitTests/Simulations/UpdateSimulationValidatorTests.cs
```

**Structure Decision**: Web backend using Clean Architecture. New files live under the existing Api, Application, Infrastructure, Domain projects; tests under parallel test projects.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
