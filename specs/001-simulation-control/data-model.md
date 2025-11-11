# Data Model: Weather Simulation Control Module

**Feature**: 001-simulation-control  
**Date**: 2025-11-10  
**Purpose**: Database schema and entity definitions

## Entity: Simulation

Represents a weather data streaming simulation configuration stored in the system.

### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key, Identity, Not Null | Auto-generated unique identifier for the simulation |
| Name | string | Max Length: 70, Not Null | Descriptive name of the simulation (e.g., "Winter Storm 2025") |
| StartTime | DateTime | Not Null, UTC | The scheduled start time for the simulation in UTC format |
| FileName | string | Max Length: 260, Not Null | Full path to the CSV data source file including directory and filename |
| Status | string | Max Length: 20, Not Null, Default: "Not Started" | Current execution status of the simulation |

### Status Values

The Status field is an enumeration with the following allowed values:

- **Not Started**: Simulation has been created but not yet begun execution (default for new records)
- **In Progress**: Simulation is currently running
- **Completed**: Simulation has finished execution

### Validation Rules

**Name Field**:
- Required (cannot be null or empty)
- Maximum length: 70 characters
- Can contain: letters, numbers, spaces, special characters (emojis supported up to 70 char limit)

**StartTime Field**:
- Required (cannot be null)
- Must be valid DateTime value
- Stored in UTC timezone (converted from ISO 8601 input during request processing)
- No restriction on past/future dates (edge case: far past/future dates accepted)

**FileName Field**:
- Required (cannot be null or empty)
- Maximum length: 260 characters (Windows MAX_PATH limit)
- Must contain only: alphanumeric characters, spaces, hyphens, underscores, periods, backslashes
- Filename component cannot start with a numeric digit
- Must reference an existing, accessible file on the file system
- Cannot be in use by another simulation with Status "In Progress" (enforced at application layer)

**Status Field**:
- Required (cannot be null)
- Set to "Not Started" automatically on creation (not provided by client)
- Must be one of the enumeration values
- Defaults to "Not Started" at database level

### Indexes

| Index Name | Columns | Type | Purpose |
|------------|---------|------|---------|
| IX_Simulations_Status | Status | Non-clustered | Optimize queries checking for in-progress simulations |
| IX_Simulations_FileName_Status | FileName, Status | Non-clustered | Optimize concurrent file usage validation queries |
| IX_Simulations_StartTime | StartTime | Non-clustered | Support future queries for simulation scheduling |

### Database Schema (SQL)

```sql
CREATE TABLE Simulations (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(70) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    FileName NVARCHAR(260) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Not Started',
    CONSTRAINT PK_Simulations PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Simulations_Status CHECK (Status IN ('Not Started', 'In Progress', 'Completed'))
);

CREATE NONCLUSTERED INDEX IX_Simulations_Status 
    ON Simulations (Status);

CREATE NONCLUSTERED INDEX IX_Simulations_FileName_Status 
    ON Simulations (FileName, Status);

CREATE NONCLUSTERED INDEX IX_Simulations_StartTime 
    ON Simulations (StartTime);
```

## Entity Framework Core Configuration

### Simulation Entity Class (Domain Layer)

```csharp
namespace WeatherStreamer.Domain.Entities
{
    /// <summary>
    /// Represents a weather data streaming simulation configuration.
    /// </summary>
    public class Simulation
    {
        /// <summary>
        /// Unique identifier for the simulation (auto-generated).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Descriptive name of the simulation.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The scheduled start time for the simulation in UTC.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Full path to the CSV data source file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Current execution status of the simulation.
        /// </summary>
        public SimulationStatus Status { get; set; }
    }
}
```

### SimulationStatus Enum (Domain Layer)

```csharp
namespace WeatherStreamer.Domain.Enums
{
    /// <summary>
    /// Execution status of a weather simulation.
    /// </summary>
    public enum SimulationStatus
    {
        /// <summary>
        /// Simulation has been created but not yet begun execution.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Simulation is currently running.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Simulation has finished execution.
        /// </summary>
        Completed = 2
    }
}
```

### Entity Configuration (Infrastructure Layer)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework Core configuration for the Simulation entity.
    /// Implements explicit configuration as required by Constitution Principle XIV.
    /// </summary>
    public class SimulationConfiguration : IEntityTypeConfiguration<Simulation>
    {
        public void Configure(EntityTypeBuilder<Simulation> builder)
        {
            // Table name
            builder.ToTable("Simulations");

            // Primary key
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .ValueGeneratedOnAdd()
                .IsRequired();

            // Name property
            builder.Property(s => s.Name)
                .HasMaxLength(70)
                .IsRequired();

            // StartTime property (stored as UTC)
            builder.Property(s => s.StartTime)
                .HasColumnType("datetime2")
                .IsRequired();

            // FileName property
            builder.Property(s => s.FileName)
                .HasMaxLength(260)
                .IsRequired();

            // Status property (stored as string, converted from enum)
            builder.Property(s => s.Status)
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired()
                .HasDefaultValue(SimulationStatus.NotStarted);

            // Indexes for query optimization
            builder.HasIndex(s => s.Status)
                .HasDatabaseName("IX_Simulations_Status");

            builder.HasIndex(s => new { s.FileName, s.Status })
                .HasDatabaseName("IX_Simulations_FileName_Status");

            builder.HasIndex(s => s.StartTime)
                .HasDatabaseName("IX_Simulations_StartTime");

            // Check constraint for Status values
            builder.HasCheckConstraint(
                "CK_Simulations_Status",
                "Status IN ('NotStarted', 'InProgress', 'Completed')"
            );
        }
    }
}
```

## DTOs (Data Transfer Objects)

### CreateSimulationRequest (API Layer)

```csharp
namespace WeatherStreamer.Api.Models
{
    /// <summary>
    /// Request model for creating a new simulation.
    /// Client provides Name, StartTime, and DataSource only.
    /// Status is set server-side and not included in request.
    /// </summary>
    public class CreateSimulationRequest
    {
        /// <summary>
        /// Descriptive name of the simulation.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The start time of the simulation in ISO 8601 format (e.g., "2025-11-10T14:30:00Z").
        /// Will be converted to UTC if timezone information provided.
        /// </summary>
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the CSV data source file.
        /// </summary>
        public string DataSource { get; set; } = string.Empty;
    }
}
```

### CreateSimulationResponse (API Layer)

```csharp
namespace WeatherStreamer.Api.Models
{
    /// <summary>
    /// Response model for successful simulation creation.
    /// Returns the auto-generated simulation ID.
    /// </summary>
    public class CreateSimulationResponse
    {
        /// <summary>
        /// The unique identifier assigned to the newly created simulation.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the created simulation (echoed back for confirmation).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The start time in ISO 8601 UTC format.
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The file path of the data source.
        /// </summary>
        public string DataSource { get; set; } = string.Empty;

        /// <summary>
        /// The initial status (always "NotStarted" for new simulations).
        /// </summary>
        public string Status { get; set; } = "NotStarted";
    }
}
```

### ErrorResponse (API Layer)

```csharp
namespace WeatherStreamer.Api.Models
{
    /// <summary>
    /// Standardized error response format for all API errors.
    /// Implements Constitution Principle V (centralized error handling).
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Correlation ID for request tracing.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the error occurred (ISO 8601 UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// High-level error message (e.g., "Validation failed").
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Detailed validation errors or additional context (optional).
        /// Key = field name, Value = list of error messages for that field.
        /// </summary>
        public Dictionary<string, List<string>>? Details { get; set; }
    }
}
```

## Relationships

Currently, the Simulation entity has no relationships with other entities. Future enhancements may include:

- **SimulationResults**: One-to-many relationship for storing execution results
- **SimulationLogs**: One-to-many relationship for execution logging
- **Users**: Many-to-one relationship if authentication is added

## Migration Strategy

### Initial Migration

```bash
# Create initial migration from Infrastructure project
cd src/WeatherStreamer.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WeatherStreamer.Api

# Apply migration to database
dotnet ef database update --startup-project ../WeatherStreamer.Api
```

### Future Schema Changes

All schema modifications must be managed through EF Core migrations:

1. Modify entity class or configuration
2. Create new migration: `dotnet ef migrations add <DescriptiveName>`
3. Review generated migration code
4. Apply migration: `dotnet ef database update`
5. Commit migration files to version control

## Data Integrity Notes

**Transaction Boundaries**:
- Simulation creation wrapped in implicit EF Core transaction
- `SaveChangesAsync` either commits all changes or rolls back on error
- Ensures no partial/orphaned records in case of failures

**Concurrency Handling**:
- No optimistic concurrency control needed for creation endpoint
- Concurrent file usage check uses database query at transaction start
- Race condition window minimal (between check and insert)

**Audit Trail** (Future Enhancement):
- Consider adding CreatedAt, CreatedBy, ModifiedAt, ModifiedBy columns
- Enables tracking of who created each simulation and when
- Not in initial scope per requirements
