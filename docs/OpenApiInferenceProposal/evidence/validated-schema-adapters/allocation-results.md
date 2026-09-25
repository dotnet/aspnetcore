# Validated schema allocation evidence

The permanent `ValidatedJsonSchemaBenchmarks` and `ValidatedJsonSchemaMvcBenchmarks` benchmarks compare all paths in one BenchmarkDotNet process after endpoint plans, validators, caches, and buffer pools are warmed in `GlobalSetup`. They use `[MemoryDiagnoser]`; the matching pass-through endpoint is each framework baseline. The framework-only validator always returns the value-type success result. Valid engine cases use the same immutable schema and raw UTF-8 payload. Invalid cases include client-safe diagnostic construction.

Default-job measurement on .NET 11 after adding dialect normalization at registration:

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

Default-job MVC measurement on .NET 11 after the same change:

| Case | Allocated B/op | Delta from MVC baseline |
|---|---:|---:|
| MVC request pass-through | 3,120 | 0 |
| MVC validated request, no-op validator | 3,120 | 0 framework |
| MVC response pass-through | 1,616 | 0 |
| MVC validated response, no-op validator | 1,616 | 0 framework |

The MVC absolute values are allocations in controller invocation, model binding, and System.Text.Json input/output formatting. The equal side-by-side values demonstrate that the outer validated-schema plan adds 0 B/op after warm-up. In particular, the pooled request stream implements the memory-based asynchronous read fast path used by MVC formatters rather than falling back to the allocating `Stream` implementation.

“Framework incremental” includes endpoint selection, precomputed evidence/context/result plumbing, pooled bounded request buffering, pooled response-body feature interception, validation dispatch, and copying. It excludes Kestrel/TestServer, endpoint System.Text.Json serialization/deserialization, and third-party validator internals. Corvus uses its collector-free boolean success API; its remaining 168 B/op is inside that engine call. JsonSchema.Net parses the raw bytes into a `JsonDocument` and creates `EvaluationResults`, accounting for its unavoidable engine cost in this adapter.

Commands:

```console
source activate.sh
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*" --job Dry
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*" --job Short
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaBenchmarks*"
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaMvcBenchmarks*" --job Dry
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaMvcBenchmarks*" --job Short
dotnet run -c Release --project src/OpenApi/perf/Microbenchmarks/Microsoft.AspNetCore.OpenApi.Microbenchmarks.csproj -- --filter "*ValidatedJsonSchemaMvcBenchmarks*"
```

The final default-job reports are preserved alongside this file as `ValidatedJsonSchemaBenchmarks-report-github.md` and `ValidatedJsonSchemaMvcBenchmarks-report-github.md`.
