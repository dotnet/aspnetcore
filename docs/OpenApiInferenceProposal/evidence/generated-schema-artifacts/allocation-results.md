# Generated validated-schema allocation interpretation

> **Active interpretation page.** Return to the
> [current generated-artifact proof](current-proof.md),
> [architecture lifecycle](../../architecture.md#lifecycle-and-forbidden-work),
> or [evidence index](../README.md).

## Common method and units

The cases ran side by side in
`Microsoft.AspNetCore.OpenApi.Microbenchmarks` with BenchmarkDotNet 0.13.0,
`MemoryDiagnoser`, .NET 11, Linux x64, server GC, and an Intel Core i7-13800H.
Schemas, validators, endpoint plans, contexts, payloads, and pooled buffers were
created in `GlobalSetup`. `B/op` means managed allocation per logical operation,
not working set or artifact size.

## Registration: runtime authority versus generated binding

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Generated integration may pay a small bounded one-time startup adapter cost but must avoid schema processing and validator compilation. |
| **1. Question/hypothesis** | How much endpoint-registration work is avoided when authority and validator state are generated? |
| **2. Measured operation** | One runtime registration or generated closed-binding registration construction per BDN invocation. |
| **3. Baseline/comparator** | Runtime registration is the architectural comparator; generated registration is the proposed build-time path. |
| **4. Included work** | Runtime: exact-source parse, dialect validation, normalization, local-reference resolution, hashing, factory compilation, and registration. Generated: one type-erased endpoint registration object. |
| **5. Excluded work** | Build/source generation, request execution, server/STJ, process startup/JIT beyond warmed BDN, and validator payload evaluation. |
| **6. Method/environment** | Warm BenchmarkDotNet default-job means in the common environment above; one observable registration per operation. |
| **7. Units** | Mean nanoseconds and managed B/op per registration. |
| **8. Acceptance criterion/budget** | Generated registration performs no schema processing/validator compile; its bounded adapter allocation must not grow beyond the required registration object. No fixed time budget. |
| **9. Observed result** | Runtime registration: 3,010.09 ns and 4,088 B/op. Generated registration: 23.04 ns and 80 B/op, a 4,008 B/op reduction. |
| **10. Interpretation/permitted conclusion** | The generated path moves measured schema preparation out of endpoint construction; the remaining 80 B is the type-erased registration object. |
| **11. Non-conclusion/caveat** | This is one-time registration, not request throughput, cold process startup, or source-generation/build cost. |

## Warm successful request/response wrapper

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Adding enforcement must not add warmed successful-request framework allocations; engine allocation remains a separate choice. |
| **1. Question/hypothesis** | Do runtime and generated registrations share a 0 B/op incremental ASP.NET wrapper after warm-up? |
| **2. Measured operation** | One pass-through, runtime-artifact request/response, or generated-artifact request/response framework-wrapper call per BDN invocation. |
| **3. Baseline/comparator** | Matching pass-through wrapper. Runtime and generated registrations are side-by-side alternatives. |
| **4. Included work** | Endpoint selection, precomputed context/result plumbing, pooled bounded request buffer, pooled response interception, validation dispatch, and copy/rewind. |
| **5. Excluded work** | Server/TestServer, endpoint STJ serialization/deserialization, validator-engine allocation, schema generation/compilation, process startup, and diagnostics. |
| **6. Method/environment** | Warm BenchmarkDotNet default-job means in the common environment above. |
| **7. Units** | Mean nanoseconds and managed B/op per framework-wrapper operation. |
| **8. Acceptance criterion/budget** | Exactly 0 B/op incremental framework allocation. No fixed latency budget. |
| **9. Observed result** | Pass-through: 13.18 ns, 0 B/op. Runtime request/response: 131.17/201.43 ns, 0 B/op. Generated request/response: 117.10/186.25 ns, 0 B/op. |
| **10. Interpretation/permitted conclusion** | Both registrations use the same allocation-free successful framework plan after warm-up; generation changes startup work, not endpoint policy. |
| **11. Non-conclusion/caveat** | The timing is not full HTTP latency and excludes server, STJ, validator-engine, invalid-diagnostic, and cold-start work. |

## Traceability

| Field | Source or result |
| --- | --- |
| Product code under test | `OpenApiValidatedJsonSchemaEndpointPlan` and generated/runtime registration in [`OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs`](../../../../src/OpenApi/src/Extensions/OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs) |
| Producer/harness code | [`ValidatedJsonSchemaBenchmarks.cs`](../../../../src/OpenApi/perf/Microbenchmarks/ValidatedJsonSchemaBenchmarks.cs): `PassThrough`, runtime/generated request/response methods, and nested `ValidatedJsonSchemaRegistrationBenchmarks` |
| Exact command | Build the linked benchmark project, then run the seven methods with `--job Dry`, `--job Short`, and the default configured job, using `--noOverwrite`, `--exporters csv`, and separate `--artifacts` directories as recorded in [`summary.txt`](results/framework-registration/summary.txt) |
| Retained output | [`summary.txt`](results/framework-registration/summary.txt), final [framework Markdown](results/framework-registration/default/20261002-112028/Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks-report-github.md) and [CSV](results/framework-registration/default/20261002-112028/Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks-report.csv), final [registration Markdown](results/framework-registration/default/20261002-112028/Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.ValidatedJsonSchemaRegistrationBenchmarks-report-github.md) and [CSV](results/framework-registration/default/20261002-112028/Microsoft.AspNetCore.OpenApi.Microbenchmarks.ValidatedJsonSchemaBenchmarks.ValidatedJsonSchemaRegistrationBenchmarks-report.csv) |
| Result mapping | Every value above maps to the identically named default-job row. An empty BDN `Allocated` cell means no managed allocation was measured and is reported here as 0 B/op. |

Dry and Short validated every selected method before the final default run. Do
not substitute the [raw same-pass reports](two-stage-annotated-demo/results/same-pass/),
which measure different artifact/exporter and Corvus operations.
