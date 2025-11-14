# quickstart.md — Delete Simulation feature

Run tests and validate the delete feature locally:

1. Start the API in Development mode:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://localhost:5000'
dotnet run --project src/WeatherStreamer.Api --no-launch-profile
```

2. Run unit and integration tests for the feature:

```powershell
# Run all tests
dotnet test

# Run only integration tests (if organized by category)
# dotnet test --filter Category=Integration
```

3. Example curl (or PowerShell Invoke-RestMethod) sequence:

- Create a Simulation (via POST or direct seeding in tests)
- GET to read the ETag header (base64 RowVersion)
- DELETE with If-Match header set to the ETag

PowerShell example (after creating a simulation with id 12):

```powershell
$etag = '"AQIDBA=="' # example quoted ETag from GET response
Invoke-RestMethod -Method Delete -Uri http://localhost:5000/api/simulations/12 -Headers @{ 'If-Match' = $etag }
```

Notes
- Tests use InMemory provider by default; ensure RowVersion seeding where necessary.
- The API treats soft-deleted rows as not found for public endpoints.
