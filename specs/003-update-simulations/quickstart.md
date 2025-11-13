# Quickstart: Implement Simulation Update (PATCH)

## 1. Add Concurrency Token to Simulation Entity
1. Modify `Simulation` entity: add `byte[] RowVersion` with `[Timestamp]` attribute.
2. Add EF configuration in `SimulationConfiguration` for concurrency token.
3. Create migration and update database.

## 2. Expose PATCH Endpoint
1. In `SimulationsController` add `PatchSimulation(int id, JsonElement body, [FromHeader(Name="If-Match")] string ifMatch)`.
2. Retrieve entity (tracked) by id; return 404 if missing.
3. Compare `ifMatch` vs `Convert.ToBase64String(entity.RowVersion)`; if mismatch return 409.
4. Map partial JSON fields to DTO; validate via `UpdateSimulationValidator` (inject current state if needed).
5. Apply domain update method enforcing transitions & immutability.
6. Save changes; on `DbUpdateConcurrencyException` return 409 with latest ETag.
7. Return 200 with updated DTO + `ETag` header.

## 3. Validation Rules (FluentValidation)
- name: optional; when present not empty, <= 70.
- status: optional; when present must follow allowed transitions.
- startTime: optional; when present and current status == Not Started must be future; reject if attempting to change after start.
- dataSource: optional; when present and current status == Not Started apply full path/file checks; reject if status != Not Started.

## 4. Audit Logging
- Create Audit helper service; log changed fields and before/after values.
- Include correlationId from existing middleware.

## 5. Testing
### Unit
- Transition rules: each legal/illegal path.
- Validator conditional checks.
### Integration
- Successful patch updating name only.
- Concurrency conflict (stale If-Match).
- Illegal status transition (Not Started → Completed).
- Immutable field change after In Progress.
- DataSource change with locked file returns 423 if detection implemented (optional if lock logic reused).

## 6. OpenAPI/Swagger
- Add `ETag` response header and `If-Match` parameter documentation.
- Ensure schema matches `openapi.update-simulations.yaml`.

## 7. Migration Commands (PowerShell)
```powershell
# Example (adjust project paths)
dotnet ef migrations add AddSimulationRowVersion -p .\backend\src\WeatherStreamer.Infrastructure -s .\backend\src\WeatherStreamer.Api
Dotnet ef database update -p .\backend\src\WeatherStreamer.Infrastructure -s .\backend\src\WeatherStreamer.Api
```

## 8. Error Responses
- 400: Validation (list field errors)
- 404: Not found
- 409: Concurrency or illegal transition
- 423: Locked DataSource (if lock logic triggered)

## 9. Rollout
- Backwards compatible; existing reads unaffected.
- Ensure migration applied before enabling endpoint.

## 10. Next Steps
- Future: Add authentication to replace `anonymous` actor.
