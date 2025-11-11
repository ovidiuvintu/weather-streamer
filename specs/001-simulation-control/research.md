# Research Document: Weather Simulation Control Module

**Feature**: 001-simulation-control  
**Date**: 2025-11-10  
**Purpose**: Technical research and architectural decisions for simulation creation API

## Technology Stack Decisions

### Decision: ASP.NET Core 8.0 Web API

**Rationale**:
- Native support for RESTful APIs with minimal boilerplate
- Built-in dependency injection container
- Excellent async/await support (required by Constitution Principle XI)
- Strong ecosystem for middleware (rate limiting, logging, error handling)
- Automatic OpenAPI/Swagger generation via Swashbuckle
- Kestrel web server optimized for high-throughput scenarios (50+ concurrent requests)

**Alternatives Considered**:
- **ASP.NET Core Minimal APIs**: Rejected due to less structure for Clean Architecture layers
- **ASP.NET Core MVC**: Rejected as too heavyweight for API-only scenarios
- **FastAPI (Python)**: Rejected to align with .NET ecosystem expertise

### Decision: Entity Framework Core 8.0

**Rationale**:
- Required by Constitution Principle XIV (EF Core best practices mandated)
- Excellent support for explicit entity configurations (`IEntityTypeConfiguration<T>`)
- Built-in migrations system for schema management
- Automatic connection pooling (Constitution Principle VI)
- Scoped lifetime per request (Constitution Principle XIV)
- Strong async support for all database operations
- In-memory provider for local development (Constitution Principle III)

**Alternatives Considered**:
- **Dapper**: Rejected due to lack of migrations and entity tracking
- **Raw ADO.NET**: Rejected due to manual connection management complexity

### Decision: SQLite for Initial Deployment

**Rationale**:
- File-based storage simplifies initial deployment on Windows
- Zero additional infrastructure requirements
- Sufficient for expected load (100-1000 simulations/day, <10 concurrent users)
- EF Core provides seamless migration path to SQL Server if needed
- Windows file system integration for CSV file validation

**Alternatives Considered**:
- **SQL Server**: Deferred until scale requires it; adds deployment complexity
- **PostgreSQL**: Rejected due to Windows deployment complexity

### Decision: FluentValidation 11.x

**Rationale**:
- Separates validation logic from DTOs (SOLID Single Responsibility Principle)
- Supports complex validation rules (file path format, length constraints)
- Integrates with ASP.NET Core model validation pipeline
- Testable validation logic independent of controllers
- Clear, readable validation rule definitions

**Alternatives Considered**:
- **Data Annotations**: Rejected due to limited expressiveness for complex rules
- **Manual validation in controllers**: Rejected as violates Clean Architecture

### Decision: Serilog for Structured Logging

**Rationale**:
- Required by Constitution Principle XIII (structured logging mandated)
- Native JSON output for log aggregation
- Rich context enrichers (correlation IDs, request paths, timestamps)
- Sinks for various outputs (Console, File, Application Insights)
- Excellent async performance
- Industry standard for .NET applications

**Alternatives Considered**:
- **Microsoft.Extensions.Logging**: Rejected as less structured output by default
- **NLog**: Rejected in favor of Serilog's more modern API and structured logging focus

### Decision: AspNetCoreRateLimit 5.x

**Rationale**:
- Proven middleware for IP-based rate limiting
- Configurable limits (100 requests/minute as specified)
- Returns HTTP 429 as required by FR-023
- Minimal performance overhead
- Easy integration with ASP.NET Core middleware pipeline

**Alternatives Considered**:
- **Custom middleware**: Rejected to avoid reinventing the wheel
- **Cloud-based rate limiting (Azure API Management)**: Deferred until cloud deployment

## Architectural Patterns

### Decision: Repository Pattern

**Rationale**:
- Abstracts data access logic from business layer (Constitution Principle XII)
- Enables unit testing of services without database dependencies
- Provides single point for database transaction management
- Aligns with Clean Architecture separation of concerns

**Implementation**:
```csharp
public interface ISimulationRepository
{
    Task<Simulation> CreateAsync(Simulation simulation, CancellationToken cancellationToken);
    Task<bool> IsFileInUseAsync(string filePath, CancellationToken cancellationToken);
}
```

### Decision: Service Layer Pattern

**Rationale**:
- Encapsulates business logic separate from API controllers (Constitution Principle I)
- Coordinates validation, repository calls, and file system checks
- Provides transactional boundaries
- Testable business logic without HTTP concerns

**Implementation**:
```csharp
public interface ISimulationService
{
    Task<int> CreateSimulationAsync(CreateSimulationRequest request, CancellationToken cancellationToken);
}
```

### Decision: Explicit Entity Configurations

**Rationale**:
- Required by Constitution Principle XIV
- Keeps entity classes clean (no data annotations pollution)
- Centralizes database schema definitions
- Easier to maintain and review

**Implementation**:
```csharp
public class SimulationConfiguration : IEntityTypeConfiguration<Simulation>
{
    public void Configure(EntityTypeBuilder<Simulation> builder)
    {
        builder.ToTable("Simulations");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(70).IsRequired();
        // ... other configurations
    }
}
```

## Validation Strategy

### Decision: Multi-Layer Validation

**Rationale**:
- Defense in depth approach ensures data integrity
- Each layer validates its own concerns

**Validation Layers**:

1. **FluentValidation (Application Layer)**:
   - Required fields presence (Name, StartTime, DataSource)
   - Field format validation (ISO 8601 dates, path length ≤260 chars)
   - Special character restrictions in file paths
   - No additional properties in JSON payload

2. **File System Validation (Infrastructure Layer)**:
   - Path existence check
   - File existence check
   - File lock detection (IOException handling → HTTP 423)
   - Concurrent file usage check (query database for in-progress simulations)

3. **Database Constraints (Domain Layer)**:
   - Primary key enforcement
   - Not null constraints
   - String length constraints (Name max 70 chars)
   - Foreign key constraints (for future features)

## Error Handling Strategy

### Decision: Global Exception Middleware

**Rationale**:
- Required by Constitution Principle V (centralized error handling)
- Consistent error response format across all endpoints
- Catches unhandled exceptions before they escape to client
- Logs all errors with correlation IDs

**Error Response Format**:
```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:00Z",
  "statusCode": 400,
  "error": "Validation failed",
  "details": {
    "Name": ["Name is required"],
    "DataSource": ["File path exceeds maximum length of 260 characters"]
  }
}
```

**HTTP Status Code Mapping**:
- 201: Successful creation
- 400: Validation errors, malformed JSON, invalid file paths
- 404: File not found
- 409: Concurrent file usage conflict
- 423: File locked by another process
- 429: Rate limit exceeded
- 500: Database errors, unexpected exceptions

## Performance Optimizations

### Decision: Async/Await Throughout

**Rationale**:
- Required by Constitution Principle XI (NON-NEGOTIABLE)
- Maximizes thread pool efficiency under load
- Enables 50+ concurrent requests without thread starvation

**Critical Async Operations**:
- All database calls (`SaveChangesAsync`, `AnyAsync`, `AddAsync`)
- File I/O operations (`File.ExistsAsync`, exception handling)
- Rate limit checks
- Logging operations

### Decision: Database Connection Pooling

**Rationale**:
- Automatic in EF Core (Constitution Principle VI)
- Reduces connection overhead for frequent requests
- Configured via connection string

**Configuration**:
```
Data Source=weather-streamer.db;Pooling=True;Max Pool Size=100;Min Pool Size=5
```

### Decision: No Caching for Write Endpoint

**Rationale**:
- POST operations don't benefit from response caching
- Each request creates new data (not idempotent)
- Rate limiting already controls request volume

## Security Considerations

### Decision: No Authentication (As Specified)

**Rationale**:
- FR-002 explicitly states no authentication required
- Internal system with trusted operators
- Future enhancement if external access needed

**Mitigation**:
- Rate limiting prevents abuse (100 req/min per IP)
- Input validation prevents injection attacks
- Structured logging tracks all operations for audit

### Decision: HTTPS Enforcement in Production

**Rationale**:
- Constitution Principle IV requires HTTPS in production
- Prevents man-in-the-middle attacks
- Standard security practice

**Implementation**:
```csharp
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

## Testing Strategy

### Decision: Three-Tier Testing Approach

**Rationale**:
- Constitution Principle VII requires comprehensive testing
- Each tier tests different concerns

**Test Tiers**:

1. **Unit Tests** (fast, isolated):
   - Service business logic (mocked repository)
   - Validator logic (FluentValidation rules)
   - Entity configurations

2. **Integration Tests** (slower, realistic):
   - Full request pipeline (controller → service → repository → database)
   - In-memory database (EF Core InMemory provider)
   - Middleware integration (error handling, rate limiting)

3. **Contract Tests** (specification validation):
   - OpenAPI schema validation
   - Response format verification
   - HTTP status code correctness

### Decision: xUnit as Test Framework

**Rationale**:
- Modern, extensible .NET test framework
- Excellent async test support
- Parallel test execution by default (faster test runs)
- Strong community and tooling support

## Logging and Observability

### Decision: Correlation ID Middleware

**Rationale**:
- Required by Constitution Principle XIII
- Enables request tracing across all layers
- Critical for debugging concurrent requests

**Implementation**:
```csharp
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Response.Headers.Add("X-Correlation-ID", correlationId);
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

### Decision: Structured Log Events

**Rationale**:
- Enables log aggregation and querying
- Captures rich context for troubleshooting

**Key Log Events**:
- Request received (INFO): correlation ID, endpoint, payload summary
- Validation failed (WARN): validation errors, correlation ID
- File validation (INFO/ERROR): path checked, result
- Database operation (INFO/ERROR): operation type, duration, result
- Response sent (INFO): status code, duration, correlation ID

## Deployment Considerations

### Decision: EF Core Migrations for Schema Management

**Rationale**:
- Required by Constitution Principle XIV
- Version-controlled schema changes
- Automated deployment via `dotnet ef database update`

**Migration Strategy**:
```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project src/WeatherStreamer.Infrastructure

# Apply to database
dotnet ef database update --project src/WeatherStreamer.Infrastructure
```

### Decision: Health Check Endpoint

**Rationale**:
- Required by Constitution Principle IX
- Enables monitoring of API and database status
- Standard practice for production deployments

**Implementation**:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WeatherStreamerDbContext>();

app.MapHealthChecks("/health");
```

## Open Questions Resolved

All technical clarifications from the feature specification have been researched and decisions documented above. No remaining unknowns block implementation.

## References

- ASP.NET Core Documentation: https://learn.microsoft.com/aspnet/core/
- Entity Framework Core: https://learn.microsoft.com/ef/core/
- Serilog Best Practices: https://github.com/serilog/serilog/wiki/Best-Practices
- Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- REST API Design: https://restfulapi.net/
