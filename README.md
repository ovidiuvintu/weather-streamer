# Weather Streamer

A robust ASP.NET Core Web API for managing weather simulation data streams with comprehensive validation and error handling.

## Features

- **Simulation Management**: Create and manage weather simulations with validated data sources
- **File Validation**: Comprehensive validation of data source files including existence, accessibility, and concurrent usage checks
- **Input Validation**: FluentValidation-powered request validation with detailed error messages
- **Error Handling**: Graceful handling of file access errors, database errors, and validation failures
- **Rate Limiting**: IP-based rate limiting (100 requests/minute) to prevent abuse
- **Health Checks**: Database connectivity monitoring at `/health` endpoint
- **Structured Logging**: Serilog-based logging with correlation IDs for request tracing
- **Clean Architecture**: Four-layer architecture (API → Application → Infrastructure → Domain) with clear separation of concerns

## Architecture

```
├── src/
│   ├── WeatherStreamer.Api/              # HTTP endpoints, middleware, configuration
│   ├── WeatherStreamer.Application/       # Business logic, DTOs, validation rules
│   ├── WeatherStreamer.Infrastructure/    # Data access, file operations, external services
│   └── WeatherStreamer.Domain/            # Core business entities and enums
└── tests/
    ├── WeatherStreamer.UnitTests/         # Unit tests with mocking
    └── WeatherStreamer.IntegrationTests/  # End-to-end API tests
```

## Prerequisites

- .NET 8.0 SDK or later
- SQLite (included with .NET)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/ovidiuvintu/weather-streamer.git
cd weather-streamer
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Apply Database Migrations

```bash
dotnet ef database update --project src/WeatherStreamer.Infrastructure --startup-project src/WeatherStreamer.Api
```

### 4. Run the Application

```bash
dotnet run --project src/WeatherStreamer.Api
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### 5. Test the Health Check

```bash
curl https://localhost:5001/health
```

## API Usage

### Create a Simulation

**Endpoint**: `POST /api/simulations`

**Request Headers**:
```
Content-Type: application/json
```

**Request Body**:
```json
{
  "name": "Winter Storm 2025",
  "startTime": "2025-01-15T10:30:00Z",
  "dataSource": "C:\\test-data\\sample.csv"
}
```

**Success Response** (201 Created):
```json
{
  "id": 1,
  "name": "Winter Storm 2025",
  "startTimeUtc": "2025-01-15T10:30:00Z",
  "dataSource": "C:\\test-data\\sample.csv",
  "status": "NotStarted"
}
```

**Response Headers**:
```
Location: /api/simulations/1
X-Correlation-ID: 550e8400-e29b-41d4-a716-446655440000
```

### Validation Rules

- **Name**: Required, max 70 characters
- **StartTime**: Required, ISO 8601 format (e.g., `2025-01-15T10:30:00Z` or `2025-01-15T10:30:00-05:00`)
- **DataSource**: Required, max 260 characters, must not start with a digit, alphanumeric + `:\\_-.` only

### Error Responses

**400 Bad Request** - Invalid input or directory not found:
```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "statusCode": 400,
  "error": "Bad Request",
  "details": {
    "Name": ["Name is required"],
    "StartTime": ["StartTime must be a valid ISO 8601 date-time"]
  }
}
```

**404 Not Found** - Data source file does not exist:
```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "statusCode": 404,
  "error": "Not Found",
  "details": {
    "Message": ["The file 'C:\\data\\missing.csv' was not found"]
  }
}
```

**409 Conflict** - File already in use by another simulation:
```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "statusCode": 409,
  "error": "Conflict",
  "details": {
    "Message": ["The file 'C:\\data\\weather.csv' is currently in use by another simulation"]
  }
}
```

**423 Locked** - File is locked by another process:
```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "statusCode": 423,
  "error": "Locked",
  "details": {
    "Message": ["The file is currently locked"]
  }
}
```

**429 Too Many Requests** - Rate limit exceeded:
```json
{
  "statusCode": 429,
  "message": "API calls quota exceeded! maximum admitted 100 per 1m."
}
```

**500 Internal Server Error** - Unexpected error:
```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-15T10:30:00Z",
  "statusCode": 500,
  "error": "Internal server error",
  "details": {
    "Message": ["An unexpected error occurred. Please contact support with correlation ID: 550e8400-e29b-41d4-a716-446655440000"]
  }
}
```

## Testing

### Run Unit Tests

```bash
dotnet test tests/WeatherStreamer.UnitTests
```

### Run Integration Tests

```bash
dotnet test tests/WeatherStreamer.IntegrationTests
```

### Run All Tests

```bash
dotnet test
```

## Configuration

### Database

The application uses SQLite by default. Connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=weather-streamer.db;Pooling=True;Max Pool Size=100;Min Pool Size=5"
  }
}
```

### Rate Limiting

Configure rate limits in `appsettings.json`:

```json
{
  "IpRateLimiting": {
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

### Logging

Logs are written to:
- Console (all environments)
- File: `logs/weather-streamer-{Date}.log` (daily rolling)

Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## Development

### Adding a Migration

```bash
dotnet ef migrations add MigrationName --project src/WeatherStreamer.Infrastructure --startup-project src/WeatherStreamer.Api
```

### Building for Production

```bash
dotnet publish src/WeatherStreamer.Api -c Release -o ./publish
```

## Deployment

### Prerequisites

- .NET 8.0 Runtime
- Write permissions for database file location
- Write permissions for logs directory

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:5001;http://+:5000
ConnectionStrings__DefaultConnection="Data Source=/path/to/weather-streamer.db;Pooling=True"
```

### Running in Production

```bash
cd publish
dotnet WeatherStreamer.Api.dll
```

## Monitoring

- **Health Checks**: `GET /health` - Returns database connectivity status
- **Logs**: Check `logs/weather-streamer-{Date}.log` for detailed request/error information
- **Correlation IDs**: All responses include `X-Correlation-ID` header for troubleshooting

## License

MIT License - see LICENSE file for details

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request