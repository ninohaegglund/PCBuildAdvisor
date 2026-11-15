# PCBuildAdvisor backend

An ASP.NET Core 10 API for browsing games and sample PC components, recommending a build, and evaluating selected parts.

## Run locally

1. Install the .NET 10 SDK and make a SQL Server database available.
2. Configure `ConnectionStrings:AdvisorDatabase` through development configuration or the `ConnectionStrings__AdvisorDatabase` environment variable.
3. Run:

   ```powershell
   dotnet restore PCBuildAdvisor.slnx
   dotnet run --project PCBuildAdvisor.Api --launch-profile https
   ```

Startup applies EF Core migrations and seeds missing sample games. Use a development database when running locally. The HTTPS launch profile serves the API at `https://localhost:7174`; development OpenAPI is available at `/openapi/v1.json`.

The requests in [PCBuildAdvisor.Api.http](PCBuildAdvisor.Api/PCBuildAdvisor.Api.http) provide examples for each endpoint.

## API

| Endpoint | Behavior |
| --- | --- |
| `GET /api/games` | Lists database games alphabetically. Use these IDs in recommendation requests; IDs are not fixed seed positions. |
| `GET /api/components?type=Cpu` | Lists the sample component catalog, optionally filtered by component type. |
| `POST /api/builds/recommend` | Selects a complete, priced build within the optional maximum budget, using the available specifications and CPU/GPU tiers. |
| `POST /api/builds/evaluate` | Evaluates a `ComputerBuild` containing component objects. Specifications are taken from the submitted objects; component IDs are not resolved against the catalog. |

Example recommendation request:

```json
{
  "mode": 0,
  "purpose": 0,
  "targetResolution": 0,
  "gameIds": [],
  "cpuPreference": "AMD",
  "gpuPreference": "NVIDIA",
  "maximumEstimatedBudget": 1200,
  "performanceGoal": "balanced"
}
```

Replace `gameIds` with IDs from `/api/games`, or leave it empty for a use-case recommendation.

- JSON enum values are numeric: mode `0/1/2` means GameOptimized/Custom/Manual; purpose `0/1/2/3/4/5` means Gaming/Streaming/ContentCreation/OfficeWork/Development/AiMl; resolution `0/1/2` means 1080p/1440p/4K.
- `performanceGoal` accepts `value`, `balanced`, or `high`.
- Manufacturer preferences match catalog manufacturers exactly, ignoring surrounding whitespace and letter case. Empty preferences impose no manufacturer constraint.
- Invalid options, negative budgets, and unknown game IDs return HTTP 400.
- A request with no eligible build returns HTTP 422. Its body includes `message` and `minimumEstimatedTotal`. The minimum is the cheapest eligible build for the preferences; it is null when the catalog cannot form any eligible, fully priced build.
- Budget selection prioritizes meeting the target CPU/GPU tier, then lower total cost. If the budget requires a lower tier, the response explains that compromise. Selection never silently changes a manufacturer preference.

## Evaluation results

- `isComplete` indicates that CPU, motherboard, memory, storage, PSU, and case slots contain components of the expected categories. It does not prove the machine will boot or that every physical constraint has been checked.
- `estimatedTotal` is a numeric sum of selected prices, or null if any selected price is unknown. An empty selection has a total of zero.
- `warnings` contains missing parts, incorrect component categories, known compatibility conflicts, and any recommendation tier compromise.
- `unverifiedChecks` identifies implemented checks that could not be completed because specifications are missing, including integrated graphics when no discrete GPU is selected.
- `compatibilityScore` is a legacy heuristic, not a compatibility probability. Empty builds score zero; evaluation warnings reduce it, and unverified checks cap it at 75. Recommendation tier compromises do not affect this compatibility score.

Implemented checks cover CPU socket, RAM generation and total capacity, motherboard/case fit, GPU length, cooler mounting and height, and reported PSU headroom. Generation and manual evaluation share this evaluator.

## Current limits

- Games are read from SQL; components remain 18 in-memory sample products. Prices are illustrative, with no live stock or currency conversion. Budget values must use the same units as those sample prices.
- Selection enumerates the small sample catalog. Expand it with a bounded search strategy before importing a large catalog.
- Game demand and CPU/GPU tiers are heuristics, not benchmark-backed FPS predictions. BIOS support, storage ports/slots, memory module counts, power connectors, and cooling performance are not yet checked.
- `mode` is retained as metadata; separate custom/manual generation workflows, saved builds, and retailer integration are not implemented.

## Tests

```powershell
dotnet test PCBuildAdvisor.slnx
```

Regression tests cover shared game identity, request validation, compatibility, incomplete/unknown specifications, budget boundaries, manufacturer constraints, and controller error responses. Database tests use EF Core's in-memory provider and do not require SQL Server. They do not exercise SQL Server migrations or a live HTTP host.
