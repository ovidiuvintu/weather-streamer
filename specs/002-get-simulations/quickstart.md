# Quickstart: Simulation Retrieval Endpoints

## Endpoints
1. GET /simulations
2. GET /simulations/by-start-time?start_time=ISO8601
3. GET /simulations/{id}

## Examples (PowerShell using curl alias `curl` or `Invoke-WebRequest`)

### List All
```powershell
curl http://localhost:5000/simulations | ConvertFrom-Json
```
Response 200:
```json
[
  {
    "id": 1,
    "name": "Storm A",
    "startTime": "2025-12-01T10:00:00Z",
    "fileName": "storm_a.bin",
    "status": "NotStarted"
  }
]
```

### Filter By Start Time (inclusive)
```powershell
$ts = "2025-12-01T10:00:00Z"
curl "http://localhost:5000/simulations/by-start-time?start_time=$ts" | ConvertFrom-Json
```

Invalid format:
```powershell
curl "http://localhost:5000/simulations/by-start-time?start_time=not-a-date" -StatusCode
```
Expect 400 with error payload.

### Get By ID
```powershell
curl http://localhost:5000/simulations/42 | ConvertFrom-Json
```
Not found (404):
```powershell
curl http://localhost:5000/simulations/9999 -StatusCode
```

## Error Payload (standardized)
```json
{
  "error": "NotFound",
  "message": "Simulation 9999 not found",
  "correlationId": "d4f0a7e3-..."
}
```

## Logging (internal)
Each request logs: endpoint, correlationId, durationMs, recordCount (or id).

## Notes
- start_time must be ISO 8601; timezone offsets accepted (converted to UTC)
- Empty result sets return 200 with []
- All retrievals are read-only and async
 - POST /simulations returns Location header in the form `/api/simulations/{id}` on success

## Next Steps / Future Enhancements
- Add pagination (`page`, `pageSize` or cursor) when counts grow
- Add ETag/conditional GET for caching
