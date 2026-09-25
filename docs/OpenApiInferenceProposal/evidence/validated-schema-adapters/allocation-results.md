# Validated schema allocation evidence

The permanent `ValidatedJsonSchemaBenchmarks` benchmark compares all paths in one BenchmarkDotNet process after endpoint plans, validators, caches, and buffer pools are warmed in `GlobalSetup`. It uses `[MemoryDiagnoser(displayGenColumns: false)]`; the pass-through endpoint is the baseline. The framework-only validator always returns the value-type success result. Valid engine cases use the same immutable schema and raw UTF-8 payload. Invalid cases include client-safe diagnostic construction.

Short-run development measurement on .NET 11:

| Case | Allocated B/op | Delta from relevant baseline |
|---|---:|---:|
| Pass-through endpoint | 0 | 0 |
| Validated request, no-op validator | 0 | 0 framework |
| Validated response, no-op validator | 0 | 0 framework |
| Validator no-op | 0 | 0 |
| Corvus valid | 168 | +168 engine |
| JsonSchema.Net valid | 1,616 | +1,616 engine |
| Corvus invalid diagnostics | 288 | +288 engine and diagnostics |
| JsonSchema.Net invalid diagnostics | 3,080 | +3,080 engine and diagnostics |

“Framework incremental” includes endpoint selection, precomputed evidence/context/result plumbing, pooled bounded request buffering, pooled response-body feature interception, validation dispatch, and copying. It excludes Kestrel/TestServer, endpoint System.Text.Json serialization/deserialization, and third-party validator internals. Corvus uses its collector-free boolean success API; its remaining 168 B/op is inside that engine call. JsonSchema.Net parses the raw bytes into a `JsonDocument` and creates `EvaluationResults`, accounting for its unavoidable engine cost in this adapter.

Commands:

```console
source activate.sh
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*" --job Dry
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*" --job Short
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*"
```

The final default-job report is preserved alongside this file as `ValidatedJsonSchemaBenchmarks-report-github.md`.
