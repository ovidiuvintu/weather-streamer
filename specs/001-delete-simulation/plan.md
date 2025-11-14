# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->
**Language/Version**: C# 12 / .NET 8.0 (project uses modern .NET; async-first requirement)
**Primary Dependencies**: ASP.NET Core (Web API), Entity Framework Core 8, FluentValidation, Swashbuckle (OpenAPI/Swagger)
**Storage**: SQLite for local/dev and lightweight production; design compatible with SQL Server / PostgreSQL for idempotent migrations
**Testing**: xUnit for unit and integration tests; WebApplicationFactory for API integration tests
**Target Platform**: Cross-platform server (Windows/Linux) — container-friendly (Docker)
**Project Type**: Web API backend (clean-architecture layering: Api, Application, Domain, Infrastructure)
**Performance Goals**: Typical CRUD API — p95 latency < 200ms under normal load; operations are I/O-bound and should be async
**Constraints**: Must follow Constitution (async-first, EF Core migrations, structured logging). Use RowVersion concurrency token. InMemory DB used for tests but seed RowVersion where necessary.
**Scale/Scope**: Small-to-medium deployment (thousands of simulations); feature scope limited to soft-delete behavior, audit entry creation, and necessary tests/migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Verify compliance with Weather Streamer Constitution v1.0.0:

- [ ] **Clean Architecture**: Does design separate API, Application, Infrastructure, Domain layers?
- [ ] **Database Integrity**: Are data types, constraints, keys properly defined?
- [ ] **Configuration**: Are connection strings externalized? Environment-specific configs ready?
- [ ] **Validation & Security**: Input validation planned? HTTPS/CORS configured? SQL parameterized?
- [ ] **Error Handling**: Global exception middleware planned? Consistent error responses?
- [ ] **Performance**: Caching strategy defined? Connection pooling utilized?
- [ ] **Testing**: Unit and integration test strategy documented?
- [ ] **Deployment**: Persistence strategy defined? Backup plan documented?
- [ ] **Documentation**: API docs (Swagger) planned? Health checks defined?
- [ ] **Code Quality**: SOLID principles applied? Code review process followed?
- [ ] **Async-First**: All I/O operations async? No blocking calls (.Result, .Wait())?
- [ ] **Layer Separation**: Business → Data Access only? No cross-layer violations?
- [ ] **Logging**: Structured logging with correlation IDs planned? Metrics defined?
- [ ] **Database Standards**: EF Core with migrations? Entity configs explicit? Transactions planned?

**Violations Requiring Justification**: [List any constitutional violations with detailed rationale in Complexity Tracking section]

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
