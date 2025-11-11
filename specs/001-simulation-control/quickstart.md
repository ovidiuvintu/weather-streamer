# Quickstart Guide: Weather Simulation Control Module

**Feature**: 001-simulation-control  
**Date**: 2025-11-10  
**Purpose**: Step-by-step guide to build, test, and run the simulation creation API

## Prerequisites

Before starting, ensure you have:

- **.NET 8.0 SDK** installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for version control
- **Postman** or **curl** for API testing (optional)
- **Windows 10/11** or **Windows Server 2019+** (for file path validation)

Verify installation:
```powershell
dotnet --version  # Should show 8.0.x
```

## Project Setup

### Step 1: Create Solution and Projects

```powershell
# Navigate to repository root
cd C:\Development\weather-streamer

# Create solution
dotnet new sln -n WeatherStreamer

# Create projects following Clean Architecture
dotnet new webapi -n WeatherStreamer.Api -o src/WeatherStreamer.Api
dotnet new classlib -n WeatherStreamer.Application -o src/WeatherStreamer.Application
dotnet new classlib -n WeatherStreamer.Infrastructure -o src/WeatherStreamer.Infrastructure
dotnet new classlib -n WeatherStreamer.Domain -o src/WeatherStreamer.Domain

# Create test projects
dotnet new xunit -n WeatherStreamer.UnitTests -o tests/WeatherStreamer.UnitTests
dotnet new xunit -n WeatherStreamer.IntegrationTests -o tests/WeatherStreamer.IntegrationTests

# Add projects to solution
dotnet sln add src/WeatherStreamer.Api/WeatherStreamer.Api.csproj
dotnet sln add src/WeatherStreamer.Application/WeatherStreamer.Application.csproj
dotnet sln add src/WeatherStreamer.Infrastructure/WeatherStreamer.Infrastructure.csproj
dotnet sln add src/WeatherStreamer.Domain/WeatherStreamer.Domain.csproj
dotnet sln add tests/WeatherStreamer.UnitTests/WeatherStreamer.UnitTests.csproj
dotnet sln add tests/WeatherStreamer.IntegrationTests/WeatherStreamer.IntegrationTests.csproj
```

### Step 2: Add Project References

```powershell
# API references Application
cd src/WeatherStreamer.Api
dotnet add reference ../WeatherStreamer.Application/WeatherStreamer.Application.csproj
dotnet add reference ../WeatherStreamer.Infrastructure/WeatherStreamer.Infrastructure.csproj

# Application references Domain
cd ../WeatherStreamer.Application
dotnet add reference ../WeatherStreamer.Domain/WeatherStreamer.Domain.csproj

# Infrastructure references Domain and Application
cd ../WeatherStreamer.Infrastructure
dotnet add reference ../WeatherStreamer.Domain/WeatherStreamer.Domain.csproj
dotnet add reference ../WeatherStreamer.Application/WeatherStreamer.Application.csproj

# Unit tests reference Application and Domain
cd ../../tests/WeatherStreamer.UnitTests
dotnet add reference ../../src/WeatherStreamer.Application/WeatherStreamer.Application.csproj
dotnet add reference ../../src/WeatherStreamer.Domain/WeatherStreamer.Domain.csproj

# Integration tests reference API
cd ../WeatherStreamer.IntegrationTests
dotnet add reference ../../src/WeatherStreamer.Api/WeatherStreamer.Api.csproj
```

### Step 3: Install NuGet Packages

```powershell
# API Layer packages
cd ../../src/WeatherStreamer.Api
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.1
dotnet add package Serilog.Sinks.File --version 5.0.0
dotnet add package AspNetCoreRateLimit --version 5.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.5.0

# Application Layer packages
cd ../WeatherStreamer.Application
dotnet add package FluentValidation --version 11.8.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.8.1

# Infrastructure Layer packages
cd ../WeatherStreamer.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0

# Unit Test packages
cd ../../tests/WeatherStreamer.UnitTests
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0

# Integration Test packages
cd ../WeatherStreamer.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
```

### Step 4: Configure Development Environment

Create `src/WeatherStreamer.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

## Implementation Steps

### Phase 1: Domain Layer

Create `src/WeatherStreamer.Domain/Enums/SimulationStatus.cs`:

```csharp
namespace WeatherStreamer.Domain.Enums;

public enum SimulationStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}
```

Create `src/WeatherStreamer.Domain/Entities/Simulation.cs`:

```csharp
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Domain.Entities;

public class Simulation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string FileName { get; set; } = string.Empty;
    public SimulationStatus Status { get; set; }
}
```

### Phase 2: Infrastructure Layer

Create EF Core configuration, repository, and database context as documented in `data-model.md`.

Key files:
- `Data/Configurations/SimulationConfiguration.cs`
- `Data/WeatherStreamerDbContext.cs`
- `Repositories/ISimulationRepository.cs`
- `Repositories/SimulationRepository.cs`
- `Services/FileValidationService.cs`

### Phase 3: Application Layer

Create business logic, validation, and service interfaces as documented in `research.md`.

Key files:
- `Services/ISimulationService.cs`
- `Services/SimulationService.cs`
- `Validators/CreateSimulationRequestValidator.cs`
- `Exceptions/ValidationException.cs`

### Phase 4: API Layer

Create controller, DTOs, and middleware as documented in `contracts/post-simulations.md`.

Key files:
- `Controllers/SimulationsController.cs`
- `Models/CreateSimulationRequest.cs`
- `Models/CreateSimulationResponse.cs`
- `Models/ErrorResponse.cs`
- `Middleware/GlobalExceptionMiddleware.cs`
- `Middleware/CorrelationIdMiddleware.cs`

Update `Program.cs` to wire up services, middleware, and Swagger.

## Database Setup

### Step 1: Create Initial Migration

```powershell
cd src/WeatherStreamer.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../WeatherStreamer.Api
```

### Step 2: Apply Migration (Development)

```powershell
dotnet ef database update --startup-project ../WeatherStreamer.Api
```

### Step 3: Verify Schema

```powershell
# Open SQLite database
sqlite3 weather-streamer.db

# Check schema
.schema Simulations

# Exit
.quit
```

## Running the Application

### Step 1: Run Locally

```powershell
cd src/WeatherStreamer.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 2: Access Swagger UI

Open browser to: `https://localhost:5001/swagger`

You should see the API documentation with POST /api/simulations endpoint.

### Step 3: Test with Swagger UI

1. Click on **POST /api/simulations**
2. Click **Try it out**
3. Enter sample request:

```json
{
  "name": "Test Simulation",
  "startTime": "2025-11-15T10:00:00Z",
  "dataSource": "C:\\test-data\\sample.csv"
}
```

**Note**: For testing, create the file `C:\test-data\sample.csv` first:

```powershell
mkdir C:\test-data
"timestamp,temperature,humidity" | Out-File -FilePath C:\test-data\sample.csv -Encoding UTF8
```

4. Click **Execute**
5. Verify **201 Created** response with simulation ID

### Step 4: Test with curl

```powershell
curl -X POST https://localhost:5001/api/simulations `
  -H "Content-Type: application/json" `
  -d '{
    \"name\": \"Test Simulation\",
    \"startTime\": \"2025-11-15T10:00:00Z\",
    \"dataSource\": \"C:\\\\test-data\\\\sample.csv\"
  }'
```

### Step 5: Test with Postman

1. Create new request: **POST** `https://localhost:5001/api/simulations`
2. Headers: `Content-Type: application/json`
3. Body (raw JSON):

```json
{
  "name": "Postman Test Simulation",
  "startTime": "2025-11-15T10:00:00Z",
  "dataSource": "C:\\test-data\\sample.csv"
}
```

4. Send request
5. Verify response status 201 and body contains `id`

## Testing

### Run Unit Tests

```powershell
cd tests/WeatherStreamer.UnitTests
dotnet test --logger "console;verbosity=detailed"
```

### Run Integration Tests

```powershell
cd tests/WeatherStreamer.IntegrationTests
dotnet test --logger "console;verbosity=detailed"
```

### Run All Tests

```powershell
cd C:\Development\weather-streamer
dotnet test --logger "console;verbosity=detailed"
```

### Test Coverage

```powershell
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Generate coverage report
dotnet-coverage collect "dotnet test" -f xml -o coverage.xml

# View results
# (Use Visual Studio Code coverage extensions or upload to SonarQube)
```

## Validation Checklist

Use this checklist to verify implementation completeness:

### Functional Requirements

- [ ] POST /api/simulations endpoint exposed
- [ ] No authentication required
- [ ] JSON payload validation (Name, StartTime, DataSource)
- [ ] Content-Type validation (application/json)
- [ ] Reject additional JSON properties
- [ ] FileName max 260 characters
- [ ] FileName cannot start with digit
- [ ] FileName special character validation
- [ ] Path existence check
- [ ] File existence check
- [ ] File lock detection (HTTP 423)
- [ ] Concurrent file usage prevention (HTTP 409)
- [ ] Rate limiting (100 req/min per IP, HTTP 429)
- [ ] Database persistence with auto-generated ID
- [ ] UTC timestamp conversion
- [ ] Status set to "Not Started"
- [ ] Transaction rollback on errors
- [ ] HTTP 201 with Location header
- [ ] Detailed error logging with correlation IDs

### Constitutional Compliance

- [ ] Clean Architecture (4 layers)
- [ ] Database integrity (constraints, keys)
- [ ] Configuration externalized
- [ ] FluentValidation used
- [ ] Global exception middleware
- [ ] EF Core connection pooling
- [ ] Unit and integration tests
- [ ] SQLite for development
- [ ] Swagger documentation
- [ ] Async/await throughout
- [ ] Layer separation enforced
- [ ] Serilog structured logging
- [ ] EF Core explicit configurations
- [ ] EF Core migrations

## Troubleshooting

### Issue: Database file locked

**Solution**: Stop any running instances of the application:
```powershell
Get-Process -Name WeatherStreamer.Api | Stop-Process
```

### Issue: Port already in use

**Solution**: Change port in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:5002;http://localhost:5001"
```

### Issue: Migration fails

**Solution**: Delete existing database and migrations folder, recreate:
```powershell
Remove-Item -Path src/WeatherStreamer.Infrastructure/Migrations -Recurse -Force
Remove-Item -Path weather-streamer.db
dotnet ef migrations add InitialCreate --startup-project src/WeatherStreamer.Api --project src/WeatherStreamer.Infrastructure
dotnet ef database update --startup-project src/WeatherStreamer.Api --project src/WeatherStreamer.Infrastructure
```

### Issue: File validation fails

**Solution**: Ensure test CSV file exists:
```powershell
mkdir C:\test-data -Force
"timestamp,temperature,humidity" | Out-File -FilePath C:\test-data\sample.csv -Encoding UTF8
```

## Next Steps

After completing this quickstart:

1. Review `/speckit.tasks` output for detailed implementation tasks
2. Implement additional user stories (if any)
3. Add health check endpoint (`/health`)
4. Configure production database (SQL Server)
5. Set up CI/CD pipeline
6. Deploy to staging environment
7. Load testing with 50+ concurrent requests
8. Security hardening (HTTPS enforcement, CORS)
9. Monitoring and alerting setup
10. Documentation for operators

## Useful Commands

```powershell
# Build solution
dotnet build

# Clean solution
dotnet clean

# Restore packages
dotnet restore

# Run with watch (auto-reload on changes)
cd src/WeatherStreamer.Api
dotnet watch run

# View logs
Get-Content -Path logs/weather-streamer-.log -Tail 50 -Wait

# Check rate limiting
for ($i=1; $i -le 105; $i++) {
    curl -X POST https://localhost:5001/api/simulations -H "Content-Type: application/json" -d '{"name":"Test","startTime":"2025-11-15T10:00:00Z","dataSource":"C:\\test-data\\sample.csv"}'
}
# Request 101+ should return 429

# View database
sqlite3 weather-streamer.db "SELECT * FROM Simulations;"
```

## Production Deployment

For production deployment guidance, see:
- `README.md` (root of repository)
- Deployment documentation (to be created)
- CI/CD pipeline configuration (to be created)

## Support

For issues or questions:
- Check application logs in `logs/` directory
- Review correlation IDs in error responses
- Consult `data-model.md`, `research.md`, and `contracts/post-simulations.md`
- Contact development team
