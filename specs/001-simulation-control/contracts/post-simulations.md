# API Contract: POST /api/simulations

**Operation**: Create Simulation  
**Feature**: 001-simulation-control  
**Version**: 1.0.0  
**Date**: 2025-11-10

## 1. Operation Definition

**Goal**: Create a new simulation record in the weather simulation system.

**User Story**: As a weather monitoring system operator, I want to create a new weather data simulation by providing simulation details and a CSV data source file, so that the system can validate the configuration and prepare it for execution.

## 2. Technical Specification

**HTTP Method**: `POST`

**Endpoint URL**: `/api/simulations`

**Controller/Handler**: `SimulationsController.CreateSimulation()`

```csharp
[ApiController]
[Route("api/[controller]")]
public class SimulationsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSimulation(
        [FromBody] CreateSimulationRequest request,
        CancellationToken cancellationToken)
    {
        // Implementation delegates to ISimulationService
    }
}
```

**Authentication & Authorization**: None required (internal system with trusted operators as per FR-002)

## 3. Request Details

### Request Body (Payload)

**Content Type**: `application/json` (required, validated via FR-004)

**Schema/Model**: `CreateSimulationRequest`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "CreateSimulationRequest",
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "Descriptive name of the simulation",
      "maxLength": 70,
      "minLength": 1,
      "example": "Winter Storm 2025"
    },
    "startTime": {
      "type": "string",
      "format": "date-time",
      "description": "The start time of the simulation in ISO 8601 format",
      "example": "2025-11-10T14:30:00Z"
    },
    "dataSource": {
      "type": "string",
      "description": "Full path to the CSV data source file",
      "maxLength": 260,
      "pattern": "^[a-zA-Z0-9 \\-_.\\\\]+$",
      "example": "C:\\data\\weather-2025.csv"
    }
  },
  "required": ["name", "startTime", "dataSource"],
  "additionalProperties": false
}
```

**Field Constraints**:

- **name** (string, required):
  - Not null or empty
  - Maximum 70 characters
  - Can contain letters, numbers, spaces, special characters (emojis counted toward limit)

- **startTime** (string, required):
  - ISO 8601 date-time format (e.g., `2025-11-10T14:30:00Z`)
  - Timezone information optional (converted to UTC if provided)
  - No restriction on past/future dates

- **dataSource** (string, required):
  - Full file path including directory and filename
  - Maximum 260 characters (Windows MAX_PATH)
  - Must contain only: alphanumeric, spaces, hyphens, underscores, periods, backslashes
  - Filename component cannot start with numeric digit
  - Path and file must exist and be accessible
  - Cannot be in use by another "In Progress" simulation

**Important**: The `status` field is **NOT** included in the request. The server automatically sets it to "Not Started" upon creation (per FR-003 and FR-018).

**Example Request**:

```json
{
  "name": "Winter Storm Simulation 2025",
  "startTime": "2025-12-15T08:00:00Z",
  "dataSource": "C:\\weather-data\\winter-storm-2025.csv"
}
```

### Headers

**Required Headers**:
- `Content-Type: application/json` (enforced via FR-004)

**Optional Headers**:
- `Accept: application/json` (recommended)

**Response Headers** (added by server):
- `X-Correlation-ID`: Unique identifier for request tracing
- `Location`: URI of the newly created resource (in 201 response)

## 4. Response Details and Error Handling

### Success Response (201 Created)

**Condition**: Simulation record created successfully in database

**Status Code**: `201 Created`

**Headers**:
```
Location: https://api.example.com/api/simulations/123
X-Correlation-ID: abc-123-def-456
Content-Type: application/json
```

**Response Body**:

```json
{
  "id": 123,
  "name": "Winter Storm Simulation 2025",
  "startTimeUtc": "2025-12-15T08:00:00Z",
  "dataSource": "C:\\weather-data\\winter-storm-2025.csv",
  "status": "NotStarted"
}
```

**Schema**:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "CreateSimulationResponse",
  "type": "object",
  "properties": {
    "id": {
      "type": "integer",
      "description": "Auto-generated unique identifier for the simulation"
    },
    "name": {
      "type": "string",
      "description": "Descriptive name of the simulation (echoed back)"
    },
    "startTimeUtc": {
      "type": "string",
      "format": "date-time",
      "description": "Start time converted to UTC"
    },
    "dataSource": {
      "type": "string",
      "description": "File path of the data source"
    },
    "status": {
      "type": "string",
      "enum": ["NotStarted"],
      "description": "Initial status (always NotStarted for new simulations)"
    }
  },
  "required": ["id", "name", "startTimeUtc", "dataSource", "status"]
}
```

### Error Responses

All error responses follow a consistent format as defined by Constitution Principle V (Centralized Error Handling).

**Error Response Schema**:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "ErrorResponse",
  "type": "object",
  "properties": {
    "correlationId": {
      "type": "string",
      "description": "Correlation ID for request tracing"
    },
    "timestamp": {
      "type": "string",
      "format": "date-time",
      "description": "When the error occurred (ISO 8601 UTC)"
    },
    "statusCode": {
      "type": "integer",
      "description": "HTTP status code"
    },
    "error": {
      "type": "string",
      "description": "High-level error message"
    },
    "details": {
      "type": "object",
      "additionalProperties": {
        "type": "array",
        "items": {
          "type": "string"
        }
      },
      "description": "Field-specific validation errors (optional)"
    }
  },
  "required": ["correlationId", "timestamp", "statusCode", "error"]
}
```

#### 400 Bad Request

**Conditions**:
- Content-Type is not `application/json` (FR-004)
- JSON deserialization fails (FR-005)
- Additional properties in JSON payload (FR-005a)
- Required field missing (FR-006)
- Status field included in request (FR-006)
- Invalid ISO 8601 date format
- FileName exceeds 260 characters (FR-007)
- FileName starts with numeric digit (FR-008)
- FileName contains invalid special characters (FR-008a)
- File path does not exist (FR-009)

**Example Response**:

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 400,
  "error": "Validation failed",
  "details": {
    "Name": ["Name is required and cannot be empty"],
    "StartTime": ["StartTime must be in ISO 8601 format (e.g., 2025-11-10T14:30:00Z)"],
    "DataSource": ["File path exceeds maximum length of 260 characters"]
  }
}
```

#### 404 Not Found

**Condition**: DataSource file does not exist at specified path (FR-010)

**Example Response**:

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 404,
  "error": "File not found",
  "details": {
    "DataSource": ["The file 'C:\\data\\missing.csv' does not exist"]
  }
}
```

#### 409 Conflict

**Condition**: DataSource file is already in use by another simulation with status "In Progress" (FR-011)

**Example Response**:

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 409,
  "error": "Resource conflict",
  "details": {
    "DataSource": ["The file 'C:\\data\\weather.csv' is currently in use by simulation ID 42 which is In Progress"]
  }
}
```

#### 423 Locked

**Condition**: DataSource file is locked by another process on the file system (FR-010a)

**Example Response**:

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 423,
  "error": "File locked",
  "details": {
    "DataSource": ["The file 'C:\\data\\weather.csv' is currently locked by another process. Please retry later."]
  }
}
```

#### 429 Too Many Requests

**Condition**: Rate limit exceeded (100 requests per minute from same IP address per FR-023)

**Headers**:
```
Retry-After: 30
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1699623600
```

**Example Response**:

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 429,
  "error": "Rate limit exceeded",
  "details": {
    "Message": ["Rate limit of 100 requests per minute exceeded. Please retry after 30 seconds."]
  }
}
```

#### 500 Internal Server Error

**Conditions**:
- Database connection failure (FR-015)
- Database transaction failure (FR-019)
- Unexpected exceptions

**Example Response** (production mode - limited details):

```json
{
  "correlationId": "abc-123-def-456",
  "timestamp": "2025-11-10T14:30:15Z",
  "statusCode": 500,
  "error": "Internal server error",
  "details": {
    "Message": ["An unexpected error occurred. Please contact support with correlation ID: abc-123-def-456"]
  }
}
```

**Note**: Detailed exception information is logged server-side with full stack traces but NOT exposed to clients in production (per Constitution Principle V).

## 5. Implementation Notes

### Dependencies

**Required Services (via Dependency Injection)**:
- `ISimulationService` - Business logic orchestration
- `ILogger<SimulationsController>` - Structured logging

**Service Dependencies**:
- `ISimulationRepository` - Database operations
- `IFileValidationService` - File system validation
- `IValidator<CreateSimulationRequest>` - FluentValidation

### Database Operations

**Transaction Scope**: Entire operation wrapped in implicit EF Core transaction

**Operations**:
1. Query database for concurrent file usage check:
   ```csharp
   var isInUse = await _dbContext.Simulations
       .AnyAsync(s => s.FileName == request.DataSource && 
                      s.Status == SimulationStatus.InProgress,
                 cancellationToken);
   ```

2. Create and add new Simulation entity:
   ```csharp
   var simulation = new Simulation
   {
       Name = request.Name,
       StartTime = DateTime.Parse(request.StartTime).ToUniversalTime(),
       FileName = request.DataSource,
       Status = SimulationStatus.NotStarted
   };
   _dbContext.Simulations.Add(simulation);
   ```

3. Save changes (commits transaction):
   ```csharp
   await _dbContext.SaveChangesAsync(cancellationToken);
   ```

**Rollback**: Automatic on any exception via EF Core transaction management

### Business Logic/Edge Cases

**Edge Case Handling**:

1. **Special characters in Name**: Accepted (including emojis) up to 70 characters
2. **Dates far in past/future**: Accepted without validation
3. **Concurrent simulations with different files**: Allowed
4. **Multiple spaces in file path**: Allowed (valid Windows paths)
5. **Name exactly 70 characters**: Accepted
6. **File locked by another process**: Detected via IOException during file existence check → HTTP 423
7. **Database connection lost mid-transaction**: Caught by exception middleware → HTTP 500, transaction rolled back

**Validation Order** (fail-fast approach):
1. Content-Type check
2. JSON deserialization
3. Additional properties check
4. Required fields presence
5. Field format validation (ISO 8601, length limits, character restrictions)
6. File path validation (special chars, numeric prefix, length)
7. Path existence check
8. File existence check
9. File lock check
10. Concurrent file usage check (database query)
11. Database insertion

### Logging Events

**Key Log Points** (with correlation IDs):

- **INFO**: Request received
  ```json
  { "Event": "SimulationCreationRequested", "CorrelationId": "...", "RequestBody": "..." }
  ```

- **DEBUG**: Validation started

- **WARN**: Validation failed (includes failed rules)

- **DEBUG**: File system checks started

- **ERROR**: File validation failed (path/file not found, locked)

- **INFO**: Database operation started

- **ERROR**: Database operation failed (with exception details)

- **INFO**: Simulation created successfully
  ```json
  { "Event": "SimulationCreated", "CorrelationId": "...", "SimulationId": 123, "Duration": "1.234s" }
  ```

## 6. Testing Plan

### Unit Tests

**SimulationServiceTests.cs**:
- `CreateSimulationAsync_WithValidRequest_ReturnsSimulationId`
- `CreateSimulationAsync_WithConcurrentFileUsage_ThrowsConflictException`
- `CreateSimulationAsync_WithDatabaseError_ThrowsAndLogsException`

**CreateSimulationRequestValidatorTests.cs**:
- `Validate_WithMissingName_ReturnsValidationError`
- `Validate_WithInvalidStartTime_ReturnsValidationError`
- `Validate_WithFileNameOver260Chars_ReturnsValidationError`
- `Validate_WithFileNameStartingWithDigit_ReturnsValidationError`
- `Validate_WithInvalidSpecialChars_ReturnsValidationError`
- `Validate_WithValidRequest_PassesValidation`

**FileValidationServiceTests.cs**:
- `ValidateFileAsync_WithNonExistentPath_ThrowsException`
- `ValidateFileAsync_WithNonExistentFile_ThrowsException`
- `ValidateFileAsync_WithLockedFile_ThrowsLockedException`
- `ValidateFileAsync_WithValidFile_ReturnsSuccess`

### Integration Tests

**SimulationsControllerTests.cs**:
- `POST_Simulations_WithValidRequest_Returns201Created`
- `POST_Simulations_WithMissingName_Returns400BadRequest`
- `POST_Simulations_WithInvalidJSON_Returns400BadRequest`
- `POST_Simulations_WithAdditionalProperties_Returns400BadRequest`
- `POST_Simulations_WithNonExistentFile_Returns404NotFound`
- `POST_Simulations_WithConcurrentFileUsage_Returns409Conflict`
- `POST_Simulations_WithDatabaseError_Returns500InternalServerError`
- `POST_Simulations_RateLimitExceeded_Returns429TooManyRequests`
- `POST_Simulations_VerifyCorrelationIdInResponse`
- `POST_Simulations_VerifyLocationHeaderIn201Response`

### Manual Testing

**Tools**: Postman, curl, Swagger UI

**Test Scenarios**:
1. Valid request → verify 201 response and database record
2. Missing required fields → verify 400 with field-specific errors
3. Invalid file path → verify 404 response
4. Concurrent file usage (requires manual database setup) → verify 409
5. Rate limiting (send 101 requests rapidly) → verify 429 on 101st request
6. Correlation ID tracking → verify same ID in response header and logs

## 7. OpenAPI/Swagger Documentation

**Auto-generated** via Swashbuckle.AspNetCore:

```yaml
openapi: 3.0.1
info:
  title: Weather Streamer API
  version: '1.0'
paths:
  /api/simulations:
    post:
      tags:
        - Simulations
      summary: Create a new weather simulation
      operationId: CreateSimulation
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateSimulationRequest'
      responses:
        '201':
          description: Simulation created successfully
          headers:
            Location:
              schema:
                type: string
              description: URI of the created resource
            X-Correlation-ID:
              schema:
                type: string
              description: Request correlation identifier
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CreateSimulationResponse'
        '400':
          description: Validation failed
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '404':
          description: File not found
        '409':
          description: Resource conflict
        '423':
          description: File locked
        '429':
          description: Rate limit exceeded
        '500':
          description: Internal server error
components:
  schemas:
    CreateSimulationRequest:
      type: object
      required: [name, startTime, dataSource]
      additionalProperties: false
      properties:
        name:
          type: string
          maxLength: 70
        startTime:
          type: string
          format: date-time
        dataSource:
          type: string
          maxLength: 260
    CreateSimulationResponse:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        startTimeUtc:
          type: string
          format: date-time
        dataSource:
          type: string
        status:
          type: string
          enum: [NotStarted]
    ErrorResponse:
      type: object
      properties:
        correlationId:
          type: string
        timestamp:
          type: string
          format: date-time
        statusCode:
          type: integer
        error:
          type: string
        details:
          type: object
          additionalProperties:
            type: array
            items:
              type: string
```

## 8. Performance Considerations

**Expected Performance**:
- Response time: <2 seconds (p95) under normal load
- Throughput: 50+ concurrent requests without degradation
- Rate limit: 100 requests/minute per IP

**Optimization Strategies**:
- Async/await throughout (non-blocking I/O)
- EF Core connection pooling (automatic)
- Indexed database queries for concurrent file usage check
- Fail-fast validation (reject early, minimize processing)

**Monitoring**:
- Response time metrics captured per request
- Database query duration logged
- Rate limit violations tracked
- Correlation IDs enable end-to-end request tracing
