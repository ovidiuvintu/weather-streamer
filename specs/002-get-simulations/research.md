# Phase 0 Research: GET Simulation Retrieval

## Objectives
Confirm assumptions, identify risks, and document deferred concerns before implementation.

## Existing Assets
- Simulation entity already defined (fields: Id, Name, StartTime UTC, FileName, Status enum)
- EF Core DbContext configured (SQLite provider)
- Logging & correlation ID middleware in place (per constitution)
- Global exception handling and standardized error response assumed from prior feature
- Swagger/OpenAPI generation active

## Assumptions (Accepted)
- Dataset size currently small (<5k rows) -> full retrieval acceptable without pagination now
- StartTime column exists and stores UTC or convertible timestamps
- No soft-delete semantics; all simulations are visible
- Public read access acceptable (no auth required) for monitoring scenarios

## Risks
| Risk | Impact | Mitigation | Decision |
|------|--------|-----------|----------|
| Lack of index on StartTime | Filter queries degrade as table grows | Add migration to create index if absent | Investigate at Phase 1; add migration task if missing |
| Large unpaginated responses future (>10k rows) | Memory pressure, latency | Introduce paging (limit + offset or cursor) | Deferred (documented in FR-016) |
| Timezone ambiguity in start_time query | Incorrect filtering | Always parse with DateTimeOffset, convert to UTC | Implement now |
| Inconsistent ordering if not enforced | Flaky tests / UX | Always OrderBy StartTime then Id | Implement now |
| Over-fetching columns | Slight CPU/IO overhead | Project to DTO with needed fields only | Implement now |

## Deferred Items
- Pagination (cursor or page/size) -> Future feature
- Max response size enforcement -> Future guardrail
- Caching (e.g., for read-heavy stable datasets) -> Evaluate after traffic observation

## Alternatives Considered
| Option | Pros | Cons | Decision |
|-------|------|------|----------|
| Raw SQL queries | Potential micro performance gains | Violates abstraction & maintainability | Rejected |
| Adding paging now | Future-proof early | Added complexity & test surface | Deferred |
| Include total count header | Useful for clients | Requires double-query without paging | Deferred |

## Performance Expectations
- Simple EF Core LINQ queries -> single roundtrip
- For ≤500 rows expected <250ms typical on dev hardware; latency target generous (<500–600ms)
- Ensure async usage to avoid thread pool blocking

## Performance Smoke Test (manual)
Run a light concurrency check after deployment to validate SC-006 using a simple HTTP load tool:
- 50 concurrent requests for 60s across the three endpoints (/simulations, /simulations/by-start-time, /simulations/{id})
- Track success rate and median latency; expect ≥95% success, median <200ms on dev
Tools: `wrk`/`hey`/`bombardier` or `k6` (script optional). Not automated in CI for this iteration.

## Validation Strategy
- start_time: required for filtered endpoint, ISO 8601 parse via DateTimeOffset.TryParse
- id: positive integer; model binding + manual check

## Logging Strategy
- Pre-request correlation ID reused
- Post-query log: { endpoint, recordCount | idRequested, durationMs, success:boolean }

## Conclusion
No blockers discovered. Proceed to Phase 1 design (data model, OpenAPI contracts, quickstart) with potential conditional task to add StartTime index if absent.
