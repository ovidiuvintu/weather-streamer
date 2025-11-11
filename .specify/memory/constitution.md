<!--
Sync Impact Report - Constitution Update
═══════════════════════════════════════
Version Change: [INITIAL] → 1.0.0
Change Type: MAJOR - Initial constitution ratification

Modified Principles:
- NEW: I. Clean Architecture (mandatory layered design)
- NEW: II. Database Integrity (data type and constraint enforcement)
- NEW: III. Configuration Management (environment-specific configs)
- NEW: IV. Validation & Security (input validation, HTTPS, CORS)
- NEW: V. Centralized Error Handling (middleware-based error management)
- NEW: VI. Performance & Scalability (caching, connection efficiency)
- NEW: VII. Testing Discipline (unit + integration tests)
- NEW: VIII. Deployment & Maintenance (persistence, backups, monitoring)
- NEW: IX. Documentation & Monitoring (API docs, health checks)
- NEW: X. Code Quality & Team Practices (code reviews, SOLID, CI/CD)
- NEW: XI. Async-First Operations (mandatory async/await, no blocking)
- NEW: XII. Strict Layer Separation (business calls data access only)
- NEW: XIII. Structured Logging & Observability (correlation IDs, metrics)
- NEW: XIV. Database Operations Standards (EF Core, migrations, transactions)

Added Sections:
- Core Principles (14 mandatory principles)
- Governance (amendment procedures, versioning policy, compliance)

Templates Requiring Updates:
✅ plan-template.md - Constitution Check section validated
✅ spec-template.md - Requirements align with principles
✅ tasks-template.md - Task categorization matches principles

Follow-up TODOs:
- None - all placeholders filled

Rationale:
This is the initial ratification establishing the foundational governance for
weather-streamer project based on clean architecture and .NET best practices.
═══════════════════════════════════════
-->

# Weather Streamer Constitution

## Core Principles

### I. Clean Architecture

The application MUST follow Clean Architecture or Onion Architecture principles to maintain clear
separation of concerns across API, Application, Infrastructure, and Domain layers.

**Rules**:
- Controllers MUST remain lightweight with business logic delegated to services
- Dependency Injection (DI) MUST be used for all repositories and services
- Database entities MUST NOT be directly exposed in API responses; use DTOs or ViewModels
- Each layer MUST have clear contracts and be independently testable

**Rationale**: Clean architecture ensures maintainability, testability, and allows the system to
evolve without tight coupling between layers. This principle makes the codebase resilient to
change and enables independent testing of business logic.

### II. Database Integrity

All database schemas MUST enforce data integrity through appropriate types and constraints.

**Rules**:
- Data types MUST be appropriate for the column's purpose (no VARCHAR for numbers, etc.)
- Primary keys MUST be defined for all entities
- Foreign keys MUST be defined for all relationships
- Unique constraints MUST be applied where business rules require uniqueness
- Check constraints MUST be used to enforce data validity rules

**Rationale**: Database-level constraints prevent invalid data from entering the system,
reducing bugs and ensuring data consistency even if application-layer validation fails.

### III. Configuration Management

Configuration MUST be externalized and environment-specific.

**Rules**:
- Connection strings MUST be stored in configuration files, never hardcoded
- Environment-specific configurations MUST exist for dev, test, and production
- Local development MUST use in-memory database by default
- Sensitive configuration MUST use secure storage mechanisms (user secrets, key vaults)

**Rationale**: Proper configuration management prevents security issues, enables seamless
deployment across environments, and simplifies local development setup.

### IV. Validation & Security

All user inputs and external data MUST be validated, and security best practices MUST be followed.

**Rules**:
- All incoming data MUST be validated using data annotations or FluentValidation
- HTTPS MUST be enforced in production environments
- CORS policies MUST be configured appropriately for the application's needs
- Detailed exception messages MUST NOT be exposed in production
- Raw SQL MUST be sanitized to prevent SQL injection (parameterized queries required)

**Rationale**: Input validation and security practices protect the application from malicious
attacks and prevent data corruption from invalid inputs.

### V. Centralized Error Handling

Error handling MUST be centralized using global exception middleware.

**Rules**:
- A global exception middleware MUST handle all unhandled exceptions
- HTTP status codes MUST be consistent and meaningful (200, 201, 400, 404, 500, etc.)
- Error responses MUST follow a consistent structure
- Sensitive error details MUST be logged server-side but not exposed to clients in production

**Rationale**: Centralized error handling ensures consistent error responses, improves
debugging efficiency, and prevents sensitive information leakage.

### VI. Performance & Scalability

The application MUST be designed for performance and horizontal scalability.

**Rules**:
- Frequently read data MUST be cached using in-memory caching or response caching
- Database connections MUST be short-lived and efficient
- Connection pooling MUST be utilized
- Resource-intensive operations MUST be identified and optimized

**Rationale**: Performance optimization from the start prevents costly refactoring later and
ensures the application can scale to meet demand.

### VII. Testing Discipline

Comprehensive testing MUST be implemented to ensure code quality and correctness.

**Rules**:
- Unit tests MUST be written for all business logic
- Integration tests MUST be written for all API endpoints
- In-memory database MUST be used for test environments
- Test data MUST be seeded consistently
- Test isolation MUST be maintained (no shared state between tests)

**Rationale**: Thorough testing catches bugs early, enables confident refactoring, and serves
as living documentation of system behavior.

### VIII. Deployment & Maintenance

Database persistence and operational reliability MUST be ensured in production environments.

**Rules**:
- Database files MUST be stored in persistent storage locations
- Database backups MUST be performed regularly
- Database file size MUST be monitored and optimized periodically
- Container deployments MUST mount database volumes to persistent storage

**Rationale**: Proper deployment and maintenance practices prevent data loss and ensure
system reliability in production environments.

### IX. Documentation & Monitoring

The system MUST be observable and well-documented.

**Rules**:
- API documentation MUST be generated automatically (Swagger/OpenAPI)
- Health check endpoints MUST be implemented for API and database status
- Application logs and metrics MUST be tracked using monitoring tools
- All public APIs MUST have clear documentation of inputs, outputs, and behaviors

**Rationale**: Documentation and monitoring enable efficient troubleshooting, onboarding of
new developers, and proactive identification of issues.

### X. Code Quality & Team Practices

Development practices MUST maintain high code quality and team collaboration.

**Rules**:
- Code reviews MUST be performed for all changes
- Consistent naming conventions MUST be followed across the codebase
- Async/await best practices MUST be followed (covered in detail in Principle XI)
- SOLID principles MUST be applied in service and repository design
- Version control (Git) MUST be used with meaningful commit messages
- CI/CD pipelines MUST be implemented for automated testing and deployment

**Rationale**: Team practices and code quality standards ensure maintainability, reduce
technical debt, and facilitate effective collaboration.

### XI. Async-First Operations (NON-NEGOTIABLE)

All I/O operations MUST be asynchronous. Synchronous blocking operations are FORBIDDEN.

**Rules**:
- All database calls MUST use async/await patterns
- All external service calls MUST be asynchronous
- All file I/O operations MUST be asynchronous
- Synchronous operations are ONLY permitted for:
  - Pure computational operations
  - Configuration reading at startup
  - Framework contracts that explicitly require synchronous execution
- No blocking calls (`.Result`, `.Wait()`) are permitted in request pipelines

**Rationale**: Asynchronous operations maximize throughput, prevent thread pool starvation,
and enable the application to scale efficiently under high load. Blocking calls in async
contexts can cause deadlocks and severe performance degradation.

### XII. Strict Layer Separation

Business layer MUST call Data Access layer, never directly across unrelated layers.

**Rules**:
- API/Controllers MUST call Application/Services layer only
- Application/Services MUST call Infrastructure/Repositories only
- Infrastructure/Repositories MUST call Database/ORM only
- Cross-layer calls (e.g., API directly to Repository) are FORBIDDEN
- Each layer MUST have clear interfaces/contracts
- Dependencies MUST flow inward (outer layers depend on inner layers, never reverse)
- DRY (Don't Repeat Yourself) and KISS (Keep It Simple) principles MUST be followed
- SOLID principles MUST be applied to all layer designs
- Design patterns (Mediator, Observer, Factory, etc.) MUST be used where appropriate

**Rationale**: Strict layer separation enforces modularity, testability, and separation of
concerns. It prevents tight coupling, makes the system easier to understand and modify, and
enables independent testing of each layer. Following SOLID and design patterns ensures the
code remains flexible and maintainable as requirements evolve.

### XIII. Structured Logging & Observability (NON-NEGOTIABLE)

Every layer MUST implement structured logging with consistent correlation tracking.

**Rules**:
- Correlation IDs MUST be generated for each request and propagated across all layers
- Log levels MUST be used appropriately:
  - ERROR: Failures requiring immediate attention
  - WARN: Degraded operation or concerning conditions
  - INFO: Business events and significant state changes
  - DEBUG: Troubleshooting data for development
- All exceptions MUST be logged with full context (stack trace, correlation ID, user context)
- Performance metrics MUST be captured for all business operations
- Structured logging format MUST be used (JSON or similar) for log aggregation
- Logs MUST NOT contain sensitive information (passwords, tokens, PII)

**Rationale**: Comprehensive logging enables rapid troubleshooting, performance analysis,
and security auditing. Correlation IDs allow tracing requests across distributed systems
and multiple layers.

### XIV. Database Operations Standards (NON-NEGOTIABLE)

All database operations MUST follow Entity Framework Core best practices.

**Rules**:
- Entity configurations MUST be explicit (using `IEntityTypeConfiguration<T>`)
- Database schema changes MUST be managed through EF Core migrations
- Connection pooling MUST be utilized (provided by EF Core by default)
- Direct SQL queries are FORBIDDEN except for:
  - Complex reporting scenarios with explicit justification
  - Performance-critical operations where EF Core query translation is inadequate
- Database context MUST be:
  - Scoped per HTTP request (via DI with scoped lifetime)
  - Disposed properly (automatic via DI container)
  - Never shared across threads or requests
- All operations requiring data consistency MUST be wrapped in transactions
- Query performance MUST be monitored using EF Core query logging
- N+1 query problems MUST be avoided (use `.Include()` for eager loading)

**Rationale**: Following EF Core best practices ensures data integrity, optimal performance,
and maintainable database access code. Proper scoping prevents concurrency issues, and
explicit configurations make the data model clear and self-documenting.

## Governance

This constitution supersedes all other development practices and guidelines for the
weather-streamer project. All code, designs, and technical decisions MUST comply with
these principles.

**Amendment Procedure**:
- Proposed amendments MUST be documented with clear rationale
- Amendments MUST include impact analysis on existing code
- Amendments MUST define migration plan if changes affect existing implementations
- Version number MUST be incremented according to semantic versioning:
  - MAJOR: Backward incompatible principle removals or redefinitions
  - MINOR: New principles added or material expansions to guidance
  - PATCH: Clarifications, wording improvements, non-semantic refinements

**Compliance Review**:
- All pull requests MUST be reviewed for constitutional compliance
- Constitution checks MUST be performed during planning phase (as defined in plan-template.md)
- Any complexity or violation MUST be explicitly justified in implementation plans
- Automated linting and CI checks SHOULD enforce constitutional rules where possible

**Documentation**:
- Runtime development guidance is provided in `.specify/templates/` directory
- Template files (plan, spec, tasks) reflect constitutional requirements
- All commands and workflows MUST align with these principles

**Version**: 1.0.0 | **Ratified**: 2025-11-10 | **Last Amended**: 2025-11-10
