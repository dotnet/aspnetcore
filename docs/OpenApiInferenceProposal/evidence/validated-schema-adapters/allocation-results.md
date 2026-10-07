# Runtime validated-schema allocation interpretation

> **Active interpretation page.** Return to the
> [runtime adapter proof](README.md), [architecture](../../architecture.md#shared-minimal-api-and-mvc-enforcement),
> or [evidence index](../README.md). Raw reports are linked only after the
> measurement boundaries.

## Common method and units

The permanent `ValidatedJsonSchemaBenchmarks` and
`ValidatedJsonSchemaMvcBenchmarks` cases run in one BenchmarkDotNet process after
endpoint plans, validators, caches, and buffer pools are warmed in
`GlobalSetup`. `MemoryDiagnoser` reports `B/op`: managed bytes allocated per
logical benchmark operation, not working set or artifact size. The recorded
default jobs used .NET 11; matching pass-through methods are the framework
baselines.

## Minimal API framework wrapper

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Adding enforcement must not add warmed successful-request/response framework allocations; engine allocation is a separate application choice. |
| **1. Question/hypothesis** | Does the precomputed Minimal API endpoint plan add managed allocation when validation succeeds? |
| **2. Measured operation** | One pass-through, validated-request, or validated-response endpoint-plan invocation per BDN operation using the no-op success validator. |
| **3. Baseline/comparator** | Matching pass-through endpoint. |
| **4. Included work** | Endpoint selection, precomputed context/result plumbing, pooled bounded request buffer, pooled response-body interception, validation dispatch, rewind/copy. |
| **5. Excluded work** | Kestrel/TestServer, endpoint STJ serialization/deserialization, validator-engine internals, schema compilation, diagnostics, process startup, and build. |
| **6. Method/environment** | Warm BenchmarkDotNet default job with `MemoryDiagnoser` on .NET 11; endpoint plan and pools initialized in `GlobalSetup`. |
| **7. Units** | Managed B/op per logical endpoint-plan operation. Time is not used for this claim. |
| **8. Acceptance criterion/budget** | Exactly 0 B/op incremental framework allocation relative to pass-through. |
| **9. Observed result** | Pass-through, valid request, and valid response each allocated 0 B/op; framework delta was 0 B/op. |
| **10. Interpretation/permitted conclusion** | The warmed successful Minimal API framework wrapper meets its incremental-allocation target. |
| **11. Non-conclusion/caveat** | This does not claim zero server, STJ, engine, invalid-diagnostic, or cold-start allocation, and it is not a latency result. |

## Validator-engine and diagnostic operations

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Framework neutrality must expose, not hide, engine-specific allocation tradeoffs behind one policy seam. |
| **1. Question/hypothesis** | What managed allocation belongs to each validator call and invalid diagnostic construction after setup? |
| **2. Measured operation** | One no-op, valid Corvus, valid JsonSchema.Net, invalid Corvus, or invalid JsonSchema.Net validator invocation per BDN operation. |
| **3. Baseline/comparator** | No-op validator is the zero-allocation framework boundary; Corvus and JsonSchema.Net are matched on the same immutable schema/payload but are not universal baselines for one another. |
| **4. Included work** | Engine evaluation and neutral result mapping; invalid cases include client-safe diagnostics. JsonSchema.Net includes `JsonDocument` and `EvaluationResults`; Corvus uses its collector-free success path. |
| **5. Excluded work** | ASP.NET endpoint wrapper, server/TestServer, STJ endpoint binding, schema compilation, process startup, and build. |
| **6. Method/environment** | Warm BenchmarkDotNet default job with `MemoryDiagnoser` on .NET 11, same schema and raw UTF-8 payload. |
| **7. Units** | Managed B/op per validator invocation. |
| **8. Acceptance criterion/budget** | Semantic equivalence is required. Lower allocation is directionally better for engine choice; no fixed engine budget. |
| **9. Observed result** | No-op: 0 B/op; Corvus valid: 168 B/op; JsonSchema.Net valid: 1,616 B/op; Corvus invalid diagnostics: 288 B/op; JsonSchema.Net invalid diagnostics: 3,080 B/op. |
| **10. Interpretation/permitted conclusion** | Engine and diagnostics allocations remain separable from the 0 B/op framework target and can inform application choice for this case. |
| **11. Non-conclusion/caveat** | These figures are not HTTP request totals, throughput rankings, cross-machine comparisons, or universal engine budgets. |

## MVC framework wrapper

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | MVC must share enforcement without adding warmed allocation beyond its existing controller/model-binding/formatter path. |
| **1. Question/hypothesis** | Does the outer validated-schema plan add managed allocation around matched MVC request/response execution? |
| **2. Measured operation** | One MVC pass-through or validated request/response operation per BDN invocation with the no-op success validator. |
| **3. Baseline/comparator** | Matching MVC request or response pass-through method. |
| **4. Included work** | MVC controller invocation, model binding, STJ input/output formatting, and the framework endpoint plan. |
| **5. Excluded work** | Kestrel/TestServer and third-party validator internals; schema compilation and process startup occur outside the operation. |
| **6. Method/environment** | Warm BenchmarkDotNet default job with `MemoryDiagnoser` on .NET 11; matching cases in one process. |
| **7. Units** | Managed B/op per MVC operation. |
| **8. Acceptance criterion/budget** | Validated and pass-through allocations must be equal; incremental plan target is 0 B/op. |
| **9. Observed result** | Request pass-through and validated request each allocated 3,120 B/op; response pass-through and validated response each allocated 1,616 B/op. |
| **10. Interpretation/permitted conclusion** | The outer validated-schema plan adds 0 B/op after warm-up, including the pooled memory-based request-stream path used by MVC formatters. |
| **11. Non-conclusion/caveat** | MVC absolute allocation is not zero and belongs largely to MVC/STJ. Equality here does not establish latency or cold-start behavior. |

## Reproduction and raw reports

The exact producer is
[`ValidatedJsonSchemaBenchmarks.cs`](../../../../src/OpenApi/perf/Microbenchmarks/ValidatedJsonSchemaBenchmarks.cs)
in
[`Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj`](../../../../src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj).
The Minimal API rows map to `PassThrough`, `ValidatedFrameworkOnly`,
`ValidatedFrameworkOnlyResponse`, `ValidatorNoOp`, `CorvusValid`,
`JsonSchemaNetValid`, `CorvusInvalidDiagnostics`, and
`JsonSchemaNetInvalidDiagnostics`. MVC rows map to the four methods on
`ValidatedJsonSchemaMvcBenchmarks`.

Run Dry and Short validation before the default job, redirecting BDN console
output and retaining the generated report. The retained Minimal API/engine
report predates the later generated-wrapper and registration methods now in the
same class, so result attribution uses only the eight stable method names listed
above rather than treating every current class member as part of that run:

```bash
source activate.sh
project=src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj
minimal_filters=(
  --filter
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.PassThrough"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.ValidatedFrameworkOnly"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.ValidatedFrameworkOnlyResponse"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.ValidatorNoOp"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.CorvusValid"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.JsonSchemaNetValid"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.CorvusInvalidDiagnostics"
  "Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.JsonSchemaNetInvalidDiagnostics"
)

dotnet run -c Release --project "$project" -- "${minimal_filters[@]}" --job Dry --noOverwrite --artifacts /tmp/openapi-runtime-dry > /tmp/openapi-runtime-dry.log 2>&1
dotnet run -c Release --project "$project" -- "${minimal_filters[@]}" --job Short --noOverwrite --artifacts /tmp/openapi-runtime-short > /tmp/openapi-runtime-short.log 2>&1
dotnet run -c Release --project "$project" -- "${minimal_filters[@]}" --noOverwrite --artifacts /tmp/openapi-runtime-final > /tmp/openapi-runtime-final.log 2>&1

dotnet run -c Release --project "$project" -- --filter "*ValidatedJsonSchemaMvcBenchmarks*" --job Dry --noOverwrite --artifacts /tmp/openapi-mvc-dry > /tmp/openapi-mvc-dry.log 2>&1
dotnet run -c Release --project "$project" -- --filter "*ValidatedJsonSchemaMvcBenchmarks*" --job Short --noOverwrite --artifacts /tmp/openapi-mvc-short > /tmp/openapi-mvc-short.log 2>&1
dotnet run -c Release --project "$project" -- --filter "*ValidatedJsonSchemaMvcBenchmarks*" --noOverwrite --artifacts /tmp/openapi-mvc-final > /tmp/openapi-mvc-final.log 2>&1
```

- [Minimal API and engine raw BDN report](ValidatedJsonSchemaBenchmarks-report-github.md)
- [MVC raw BDN report](ValidatedJsonSchemaMvcBenchmarks-report-github.md)

## Traceability

| Traceability field | Mapping |
| --- | --- |
| Product code under test | [`OpenApiValidatedJsonSchemaEndpointPlan`](../../../../src/OpenApi/src/Extensions/OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs) and validator contracts in [`OpenApiSchemaEvidence.cs`](../../../../src/OpenApi/src/Services/Schemas/OpenApiSchemaEvidence.cs) |
| Producer/harness code | `ValidatedJsonSchemaBenchmarks` and nested `ValidatedJsonSchemaMvcBenchmarks` in the linked benchmark source |
| Exact command | The filtered Dry → Short → default commands above |
| Retained output | The two linked GitHub Markdown reports; their environment headers and method rows are the result authority |
| Result mapping | Each observed figure maps to the identically named benchmark row; BDN's `Allocated` column is managed bytes per operation |
