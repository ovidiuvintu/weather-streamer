# Implementation Plan: Weather Simulation Control Module

**Branch**: `001-simulation-control` | **Date**: 2025-11-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-simulation-control/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a REST API endpoint for creating weather data simulation records. The system accepts JSON payloads with simulation name, start time, and CSV data source file path, performs comprehensive validation (file existence, path format, concurrent usage), stores simulation records in a database with auto-generated IDs, and returns appropriate HTTP status codes. The implementation follows Clean Architecture with ASP.NET Core Web API, Entity Framework Core for data access, and includes rate limiting, structured logging with correlation IDs, and comprehensive error handling.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: 
- ASP.NET Core 8.0 (Web API framework)
- Entity Framework Core 8.0 (ORM)
- Serilog (Structured logging)
- FluentValidation 11.x (Input validation)
- AspNetCoreRateLimit 5.x (Rate limiting middleware)
- Swashbuckle.AspNetCore 6.x (OpenAPI/Swagger documentation)

**Storage**: 
- Development: SQLite (in-memory via EF Core InMemory provider)
- Production: SQL Server (or SQLite file-based for initial deployment)

**Testing**: 
- xUnit 2.6.x (Unit and integration testing framework)
- Moq 4.20.x (Mocking framework)
- FluentAssertions 6.12.x (Assertion library)
- Microsoft.AspNetCore.Mvc.Testing (Integration test support)
- EF Core InMemory provider (Test database)

**Target Platform**: Windows Server 2019+ / Windows 11 (IIS or Kestrel standalone)  
**Project Type**: Web API (single backend project)  
**Performance Goals**: 
- Response time: <2 seconds for valid simulation creation (p95)
- Throughput: 50 concurrent requests without degradation
- Rate limit: 100 requests per minute per IP

**Constraints**: 
- No authentication required (internal system)
- File system access required for CSV validation
- Windows file path conventions (260 char limit, backslash separators)
- Database transactions required for data integrity

**Scale/Scope**: 
- Initial scope: Single simulation creation endpoint
- Expected load: Internal operators, <10 concurrent users typically
- Data volume: Estimated 100-1000 simulations per day

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

**Violations Requiring Justification**: None - Full constitutional compliance

**Constitution Check Results**:
- ✅ Clean Architecture: API → Application → Infrastructure → Domain layers
- ✅ Database Integrity: Primary keys, constraints, proper data types defined
- ✅ Configuration: appsettings.json with environment overrides, user secrets for dev
- ✅ Validation & Security: FluentValidation, no raw SQL, rate limiting implemented
- ✅ Error Handling: Global exception middleware with consistent error responses
- ✅ Performance: EF Core connection pooling, no caching needed for write-only endpoint
- ✅ Testing: xUnit for unit tests, integration tests with in-memory database
- ✅ Deployment: Database migrations, file-based persistence in production
- ✅ Documentation: Swagger/OpenAPI auto-generated, health check endpoint planned
- ✅ Code Quality: SOLID principles in design, async/await throughout
- ✅ Async-First: All I/O operations async (database, file system)
- ✅ Layer Separation: Controller → Service → Repository → DbContext
- ✅ Logging: Serilog with correlation IDs, structured JSON logging
- ✅ Database Standards: EF Core with explicit entity configurations and migrations

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

```text
src/
├── WeatherStreamer.Api/                    # API Layer (ASP.NET Core Web API)
│   ├── Controllers/
│   │   └── SimulationsController.cs        # POST /api/simulations endpoint
│   ├── Middleware/
│   │   ├── GlobalExceptionMiddleware.cs    # Centralized error handling
│   │   └── CorrelationIdMiddleware.cs      # Request correlation tracking
│   ├── Models/
│   │   ├── CreateSimulationRequest.cs      # Request DTO
│   │   ├── CreateSimulationResponse.cs     # Response DTO
│   │   └── ErrorResponse.cs                # Standard error format
│   ├── Program.cs                          # Application entry point
│   ├── appsettings.json                    # Configuration
│   └── appsettings.Development.json        # Dev overrides
│
├── WeatherStreamer.Application/            # Application/Business Layer
│   ├── Services/
│   │   ├── ISimulationService.cs           # Service interface
│   │   └── SimulationService.cs            # Business logic implementation
│   ├── Validators/
│   │   ├── CreateSimulationRequestValidator.cs  # FluentValidation rules
│   │   └── FilePathValidator.cs            # Custom file validation logic
│   └── Exceptions/
│       ├── ValidationException.cs          # Custom validation exception
│       └── FileAccessException.cs          # File-specific exceptions
│
├── WeatherStreamer.Infrastructure/         # Infrastructure Layer (Data Access)
│   ├── Data/
│   │   ├── WeatherStreamerDbContext.cs     # EF Core DbContext
│   │   ├── Configurations/
│   │   │   └── SimulationConfiguration.cs  # Entity configuration
│   │   └── Migrations/                     # EF Core migrations
│   ├── Repositories/
│   │   ├── ISimulationRepository.cs        # Repository interface
│   │   └── SimulationRepository.cs         # Repository implementation
│   └── Services/
│       └── FileValidationService.cs        # File system access logic
│
└── WeatherStreamer.Domain/                 # Domain Layer (Core Entities)
    ├── Entities/
    │   └── Simulation.cs                   # Domain entity
    └── Enums/
        └── SimulationStatus.cs             # Status enumeration

tests/
├── WeatherStreamer.UnitTests/              # Unit Tests
│   ├── Services/
│   │   └── SimulationServiceTests.cs
│   ├── Validators/
│   │   └── CreateSimulationRequestValidatorTests.cs
│   └── Repositories/
│       └── SimulationRepositoryTests.cs
│
└── WeatherStreamer.IntegrationTests/       # Integration Tests
    ├── Controllers/
    │   └── SimulationsControllerTests.cs
    ├── Infrastructure/
    │   └── TestWebApplicationFactory.cs    # Test server setup
    └── Fixtures/
        └── DatabaseFixture.cs              # Test data seeding
```

**Structure Decision**: Clean Architecture with 4-layer separation (API, Application, Infrastructure, Domain). This structure follows the constitution's Clean Architecture principle and ensures:
- Clear separation of concerns with defined layer boundaries
- Dependency inversion (outer layers depend on inner layers)
- Independent testability of each layer
- Easy to extend with additional features without coupling
