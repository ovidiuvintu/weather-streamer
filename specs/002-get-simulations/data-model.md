# Phase 1 Data Model: Simulation Retrieval

## Source Entity (Domain)
`Simulation` (existing):
- `Id` (int, PK)
- `Name` (string ≤70)
- `StartTime` (DateTime / DateTimeOffset UTC stored or normalized)
- `FileName` (string)
- `Status` (enum: NotStarted | InProgress | Completed)

## Retrieval DTO
`SimulationDto` (API surface):
```json
{
  "id": 123,
  "name": "Storm Simulation Alpha",
  "startTime": "2025-12-01T10:00:00Z",
  "fileName": "alpha.bin",
  "status": "InProgress"
}
```

### Mapping Rules
- `Id` -> `id`
- `Name` -> `name`
- `StartTime` -> `startTime` (UTC, ISO 8601 without sub-ms; use `o` format or `ToUniversalTime()` then `ToString("yyyy-MM-ddTHH:mm:ssZ")` if trimming ms)
- `FileName` -> `fileName`
- `Status` -> `status` (enum string)

### Ordering Logic
List endpoints MUST apply: `OrderBy(s => s.StartTime).ThenBy(s => s.Id)` BEFORE projection to DTO to avoid multiple enumerations.

### Filtering Logic
Filtered endpoint applies inclusive boundary:
```csharp
var boundaryUtc = DateTimeOffset.Parse(startTimeQuery).UtcDateTime;
query = query.Where(s => s.StartTime >= boundaryUtc);
```
(Assuming `StartTime` stored as UTC; if `Kind` unspecified, treat as UTC by convention.)

### Index Consideration
If not present, recommend migration:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_Simulations_StartTime",
    table: "Simulations",
    column: "StartTime");
```

### Validation Edge Cases
- Non-positive `Id` -> 400
- Large `Id` non-existent -> 404
- `start_time` invalid format -> 400
- `start_time` with offset -> convert to UTC, still inclusive

### Concurrency
Read-only operations; no locks required. EF Core change tracker not used for DTO projection (use `AsNoTracking()`).

### Projection Snippet
```csharp
var dtos = await _context.Simulations
    .AsNoTracking()
    .OrderBy(s => s.StartTime).ThenBy(s => s.Id)
    .Select(s => new SimulationDto
    {
        Id = s.Id,
        Name = s.Name,
        StartTime = s.StartTime, // ensure already UTC
        FileName = s.FileName,
        Status = s.Status.ToString()
    })
    .ToListAsync(cancellationToken);
```

### Error Model (reuse)
If existing `ErrorResponse` model available use it; else define:
```json
{
  "error": "NotFound",
  "message": "Simulation 9999 not found",
  "correlationId": "..."
}
```

### Future Extensions
- Add `durationSeconds` (post-completion) later
- Add pagination wrapper: `{ items: [...], totalCount, nextCursor }`
