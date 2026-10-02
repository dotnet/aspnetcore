# Current generated-artifact proof

> **Active evidence landing.** Return to [Integrations](../../integrations.md),
> [Architecture](../../architecture.md#validator-seams), or the
> [evidence index](../README.md). The
> [detailed technical record](same-pass-exporter-removal.md) preserves the full
> implementation rationale, identities, commands, and historical comparisons.

## Claim

An external producer can emit one canonical JSON Schema artifact during its
semantic pass, then pair that authority independently with JsonSchema.Net or
Corvus generated validation while ASP.NET retains one OpenAPI projection and
endpoint-enforcement policy.

## Setup

The bounded proof uses one annotated model with a nested resource and a
conditional `taxId` requirement. A production-grade illustrative
`JsonSchema.Net.Generation`/`json-everything` fork emits:

- an ordered canonical resource bundle and manifest;
- a deterministic graph identity;
- an optional private JsonSchema.Net native graph; and
- a compatibility document consumed by a build-time Corvus ProgramImage
  producer.

ASP.NET adapters expose one generated artifact and two closed bindings. Both use
the same generated registration and endpoint plan. Neither external engine is a
shipping ASP.NET dependency.

## Success criteria

1. Native JsonSchema.Net, parsed canonical resources, Corvus compile/image, both
   ASP.NET bindings, Minimal API, and MVC agree on the full valid/invalid corpus.
2. Both bindings produce identical OpenAPI 3.0, 3.1, and 3.2 output.
3. Two clean builds produce byte-identical authority, metadata, and image source.
4. Generated endpoint construction performs no schema parse, normalization,
   hashing, resolution, provider lookup, or validator compilation.
5. The successful warmed framework wrapper adds exactly 0 B/op incrementally;
   validator-engine allocation is measured separately.
6. The Corvus-only consumer trims and NativeAOT-publishes without native
   JsonSchema.Net graph initialization or references in consumer IL.

## Headline result

All behavioral and identity checks passed for the recorded model. Both engines
used one graph identity and different validator/binding identities, produced
equal OpenAPI, and converged on the same request/response policy. Clean rebuilds
were byte-identical. The generated Corvus consumer built and ran under trimming
and NativeAOT. The performance observations below quantify distinct boundaries;
they are not one ranking.

## Canonical authority and correctness

| Representative case | Required observation |
| --- | --- |
| Business payload with `taxId` and complete nested address | Valid through native graph, canonical parse, image, both bindings, Minimal API, and MVC |
| Business payload without `taxId` | Invalid everywhere; conditional semantics survive each boundary |
| Personal payload without `taxId` | Valid; condition does not become unconditional |
| Missing/incomplete/wrong-type address | Invalid; nested resource resolution remains aligned |
| Wrong-type discriminator field or malformed JSON | Invalid at engine and HTTP boundaries |
| Invalid response | Suppressed and replaced with empty 500 |
| Oversized request | Rejected by framework policy |

The observed corpus matched those expectations. Both bindings produced valid 200,
invalid-request 400, invalid-response suppression/500, equal neutral diagnostic
classes, and identical OpenAPI 3.0/3.1/3.2 output. OpenAPI 3.0 deliberately
widens semantics that its dialect cannot represent; runtime validation remains
against canonical authority.

### Traceability

| Field | Source or result |
| --- | --- |
| Product code under test | ASP.NET generation in [`ValidatedJsonSchemaGenerator`](../../../../src/OpenApi/gen/ValidatedJsonSchemaGenerator.cs) and [`ValidatedJsonSchemaGeneratorNormalizer`](../../../../src/OpenApi/gen/ValidatedJsonSchemaGeneratorNormalizer.cs), projection in [`OpenApiValidatedJsonSchemaImporter`](../../../../src/OpenApi/src/Services/Schemas/OpenApiValidatedJsonSchemaImporter.cs), and endpoint enforcement in [`OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs`](../../../../src/OpenApi/src/Extensions/OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs) |
| Producer/harness code | Stage 1 [`FlagshipModel.cs`](two-stage-annotated-demo/stage1/FlagshipModel.cs), [`AnnotatedModelStage1.csproj`](two-stage-annotated-demo/stage1/AnnotatedModelStage1.csproj), and [`Program.cs`](two-stage-annotated-demo/stage1/Program.cs); [`TwoStageAnnotatedDemo.proj`](two-stage-annotated-demo/TwoStageAnnotatedDemo.proj); Corvus [`CorvusImageProducer.csproj`](jsonschema-net-generation-inspection/corvus-image-producer/CorvusImageProducer.csproj); Stage 2 [`AnnotatedSchemaDemo.csproj`](two-stage-annotated-demo/stage2/AnnotatedSchemaDemo.csproj), [`GeneratedBindings.cs`](two-stage-annotated-demo/stage2/GeneratedBindings.cs), and [`Program.cs`](two-stage-annotated-demo/stage2/Program.cs) |
| Exact command | The `/t:Run` command under [Hands-on demo](two-stage-annotated-demo/README.md#2-build-and-run-the-proof), with the pinned Corvus checkout and verified unpublished producer package |
| Retained output | Canonical [`generated/`](two-stage-annotated-demo/generated/) files and the recorded [`build-transcript.txt`](two-stage-annotated-demo/build-transcript.txt); current repository counts are separately authoritative in [`../validation-current.txt`](../validation-current.txt) |
| Result mapping | Stage 2 methods `VerifyIdentityChain`, `VerifyEngineEquivalence`, `VerifyAspNetBindingsAsync`, `VerifyOpenApiProjectionAsync`, and `VerifyHttpPipelineAsync` respectively map identities, engine corpus, generated bindings, 3.0/3.1/3.2 projection, and Minimal API/MVC policy |

The external producer implementation is not checked into this repository. Its
maximum retained provenance is package SHA-256
`5cd990f735218993a2a2322b86032db27685f66f62ece536a4ff940115f6adee`
and `json-everything` base commit
`ff430467e33e54d56537954fae73dbdda2c95246`; this source boundary is Partial.

## How to read the performance evidence

Every result below follows the field convention in the
[evidence index](../README.md#how-to-read-evidence). `B/op` is managed allocation
per logical operation. BenchmarkDotNet observations are warmed means; the cold
harness reports fresh-process medians/means. Compare only matched operations in
the same method/environment.

### Same-pass authority versus historical exporter

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Authoritative bytes must be produced during generation, avoiding post-compilation graph reconstruction and a second trust boundary. |
| **1. Question/hypothesis** | Does same-pass emission remove active reconstruction/serialization work? |
| **2. Measured operation** | Historical `HistoricalExporterBundleConstruction` constructs/serializes the bundle; `SamePassArtifactByteAccess` reads one already-generated byte-array value per invocation. |
| **3. Baseline/comparator** | Historical exporter versus same-pass warm byte access. These are intentionally unlike operations used to demonstrate architectural removal, not a speedup ratio. |
| **4. Included work** | Exporter: native-graph reconstruction and bundle serialization. Same-pass: access to precomputed generated bytes. |
| **5. Excluded work** | Build/source generation, process startup, request validation, ASP.NET endpoint policy, and server/STJ work. |
| **6. Method/environment** | BenchmarkDotNet 0.13.0 default job, Ubuntu 22.04 under WSL, Intel Core i7-13800H, .NET 11 preview; setup outside timed operations. |
| **7. Units** | Mean time per operation (`us`/`ns`) and managed `B/op`. |
| **8. Acceptance criterion/budget** | Exporter absent from active production and same-pass access at 0 B/op. No latency budget. |
| **9. Observed result** | Historical exporter: 29.880 us and 50,360 B/op. Same-pass access: 0.294 ns and 0 B/op. |
| **10. Interpretation/permitted conclusion** | Active access is precomputed and allocation-free; reconstruction/export work is removed from the architecture. |
| **11. Non-conclusion/caveat** | The timer-floor access value is not portable latency, validator throughput, or a valid `29.880 us / 0.294 ns` speedup. |

### Cold first-operation harness

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | Cold-start consequences hidden by warmed BenchmarkDotNet must remain visible for startup-sensitive deployments. |
| **1. Question/hypothesis** | How much first-operation work remains for generated bytes, the historical exporter, and ProgramImage loading in fresh processes? |
| **2. Measured operation** | First selected operation in a newly started process; process launch itself is excluded. |
| **3. Baseline/comparator** | Same-pass first access versus historical exporter first operation; image load is an absolute observation with no matched byte-access comparator. |
| **4. Included work** | First-access JIT/module effects and the selected operation; image sample includes evaluator-image load. |
| **5. Excluded work** | Process launch, build, server/TestServer, endpoint serialization, and steady-state request throughput. |
| **6. Method/environment** | Internal harness over fresh processes on the recorded Linux/WSL/.NET 11 environment; reports median and mean because cold samples are skewed. |
| **7. Units** | Microseconds per first operation and managed bytes for that sample. |
| **8. Acceptance criterion/budget** | Active path performs no exporter work; image path performs no schema-text parse/resolution/compile. No fixed cold-time budget. |
| **9. Observed result** | Same-pass access: 152.00 us median, 165.57 us mean, 2,680 B. Historical export: 3,682.85 us median, 4,075.23 us mean, 62,160 B median. Image load: 26,001.50 us median, 26,460.79 us mean, 65,080 B. |
| **10. Interpretation/permitted conclusion** | First access to generated authority avoids historical reconstruction; image initialization cost is explicit and separately monitorable. |
| **11. Non-conclusion/caveat** | Cold medians cannot be compared directly with warmed BDN means, and image load is not request latency or bundle access. |

### Corvus compile versus ProgramImage load

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | AOT/cold-sensitive deployments should avoid runtime schema compilation and move that work to build time. |
| **1. Question/hypothesis** | Does loading a build-time image reduce matched evaluator initialization work versus compiling the same compatibility schema at runtime? |
| **2. Measured operation** | One `EngineOnlyCompileCorvus` or `EngineOnlyLoadCorvusImage` invocation after benchmark setup. |
| **3. Baseline/comparator** | Runtime Corvus compile is baseline; ProgramImage load is the alternative. |
| **4. Included work** | Evaluator compilation from schema bytes or evaluator construction from the precompiled image. |
| **5. Excluded work** | ASP.NET binding/endpoint policy, server/STJ, build-time image generation, process startup, and payload validation. |
| **6. Method/environment** | Matched BenchmarkDotNet 0.13.0 default-job methods on the recorded Linux/WSL/.NET 11 environment. |
| **7. Units** | Mean microseconds and managed B/op per evaluator initialization. |
| **8. Acceptance criterion/budget** | Same corpus behavior and compatible identity; generated binding must not compile schema at startup. Lower is directionally better; no fixed latency/allocation budget. |
| **9. Observed result** | Compile: 22.192 us and 52,600 B/op. Image load: 12.132 us and 39,160 B/op. |
| **10. Interpretation/permitted conclusion** | In this case the image removed runtime compilation and used 13,440 fewer allocated bytes during initialization. |
| **11. Non-conclusion/caveat** | The schema has no regex patterns; this does not prove generated-regex benefit, request throughput, or universal scaling. |

## Symmetric ASP.NET binding execution

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | The validator seam must permit engines with different throughput/allocation profiles without changing framework policy. |
| **1. Question/hypothesis** | Can two private engines execute behind the same artifact, validator, binding, and registration contracts without request-time schema processing? |
| **2. Measured operation** | One complete valid `AspNetJsonSchemaNetBindingValid` or `AspNetCorvusBindingValid` call per BDN invocation. |
| **3. Baseline/comparator** | Matched JsonSchema.Net and Corvus binding calls over the same schema/payload. Neither is a framework baseline. |
| **4. Included work** | Shared ASP.NET binding call, engine evaluation, and neutral result mapping; JsonSchema.Net includes UTF-8 parsing to `JsonDocument`; Corvus includes its engine call. |
| **5. Excluded work** | Server/TestServer, endpoint STJ binding/serialization, schema generation, normalization, hashing, resolution, compilation, and one-time private state initialization. |
| **6. Method/environment** | Dry and Short validation followed by BenchmarkDotNet 0.13.0 default job on the recorded Linux/WSL/.NET 11 environment. |
| **7. Units** | Mean time (`us`/`ns`) and managed B/op per complete binding call. |
| **8. Acceptance criterion/budget** | Semantic equivalence, shared graph identity, distinct configuration/binding identities, and no schema processing in the call. Lower is directionally better for engine choice; no product budget. |
| **9. Observed result** | JsonSchema.Net: 8.4335 us and 12,816 B/op. Corvus: 384.8 ns and 168 B/op. |
| **10. Interpretation/permitted conclusion** | Both engines fit the same ASP.NET seam; the measured profiles can inform application engine choice for this schema. |
| **11. Non-conclusion/caveat** | This is not universal engine superiority, framework throughput, HTTP request latency, or a cross-machine comparison. |

Raw matched reports are retained under
[`results/symmetric-bindings`](two-stage-annotated-demo/results/symmetric-bindings/).

### Performance traceability

| Field | Source or result |
| --- | --- |
| Product code under test | Generated/runtime registration and endpoint plan in [`OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs`](../../../../src/OpenApi/src/Extensions/OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions.cs) |
| Producer/harness code | [`DemoBenchmarks.cs`](two-stage-annotated-demo/stage2/DemoBenchmarks.cs) in [`AnnotatedSchemaDemo.csproj`](two-stage-annotated-demo/stage2/AnnotatedSchemaDemo.csproj), entered through `BenchmarkSwitcher` in [`Program.cs`](two-stage-annotated-demo/stage2/Program.cs); fresh-process producer in [`run-cold-probes.sh`](two-stage-annotated-demo/run-cold-probes.sh) |
| Exact command | Filtered Dry → Short → default and cold-process commands under [Focused performance reproduction](two-stage-annotated-demo/README.md#focused-performance-reproduction) |
| Retained output | Same-pass [default report](two-stage-annotated-demo/results/same-pass/final2/results/AnnotatedSchemaDemo.DemoBenchmarks-report-github.md), symmetric [default report](two-stage-annotated-demo/results/symmetric-bindings/final/results/AnnotatedSchemaDemo.DemoBenchmarks-report-github.md), and [cold-process CSV](two-stage-annotated-demo/results/same-pass/cold-process.csv) |
| Result mapping | Retained same-pass rows `NativeExporterBundle`, `SamePassBundleAccess`, `CompileCorvus`, and `LoadCorvusImage` correspond to current methods `HistoricalExporterBundleConstruction`, `SamePassArtifactByteAccess`, `EngineOnlyCompileCorvus`, and `EngineOnlyLoadCorvusImage`; symmetric rows retain their current `AspNetJsonSchemaNetBindingValid` and `AspNetCorvusBindingValid` names |

## Framework registration and warmed wrapper

Generated registration, runtime registration, and valid request/response wrapper
measurements are interpreted in
[Generated validated-schema allocation evidence](allocation-results.md). That
page defines the one-time registration operation, pass-through comparator,
included/excluded framework work, exact 0 B/op successful-wrapper target, and
the meaning of the observed 80 B generated adapter allocation.

## Deployment and footprint

| Field | Definition |
| --- | --- |
| **0. Design constraint / why measured** | The generated validator path must deploy under trimming/NativeAOT; footprint is a tradeoff that must remain observable. |
| **1. Question/hypothesis** | Can a Corvus-only generated consumer build and run under trimming and NativeAOT without native JsonSchema.Net graph use? |
| **2. Measured operation** | Publish the isolated consumer, run each executable over the valid/invalid pair, then inspect dependency metadata, IL, references, and strings. |
| **3. Baseline/comparator** | None; absolute deployability and footprint observation. |
| **4. Included work** | Self-contained runtime files in directory totals; executable/debug sizes; embedded 726 B ProgramImage; static consumer inspection. |
| **5. Excluded work** | Startup/request latency, memory working set, package download size, compression, other operating systems/architectures, and larger schemas. |
| **6. Method/environment** | `linux-x64` self-contained trimmed and NativeAOT publishes on the recorded .NET 11/Linux evidence environment. This is not a microbenchmark. |
| **7. Units** | Total bytes on disk for publish directories/files. |
| **8. Acceptance criterion/budget** | Both builds and runs succeed; consumer IL contains no native graph initialization or JsonSchema.Net reference. No absolute size budget. |
| **9. Observed result** | Current replay: trimmed directory 31,532,218 B; NativeAOT directory 14,542,108 B, including 4,268,320 B executable and 6,739,688 B debug file. Both executables printed the expected graph/image identities. `.deps.json`, managed metadata, managed strings, native symbols, and native strings had no forbidden match. |
| **10. Interpretation/permitted conclusion** | The bounded generated consumer is trim/AOT deployable, and the recorded sizes provide a scaling baseline. |
| **11. Non-conclusion/caveat** | Sizes do not prove speed or universal footprint. The prototype package still has unconditional dependencies; copied files are packaging debt, not evidence of runtime use. |

## Deterministic build and identities

**Design constraint:** generated authority and bindings must be reproducible and
must not change with machine paths, timestamps, culture, or unordered traversal.
This is correctness evidence, not a performance measurement.

Two clean builds produced byte-identical bundle, manifest, compatibility schema,
generated MSBuild properties, and ProgramImage source. Their SHA-256 values are
retained in the [detailed reference evidence](same-pass-exporter-removal.md#deterministic-build-outputs).
Any mismatch is a failure to investigate; there is no timing budget.

Current replay is **Partial**: the exact unpublished package hash used by the
retained run is no longer available. The local package has a different hash, so
it was not substituted. The retained artifact hashes and provenance check are in
[`determinism-current-status.txt`](two-stage-annotated-demo/results/determinism-current-status.txt).

## Deployment traceability

| Field | Source or result |
| --- | --- |
| Product code under test | Generated image source [`FlagshipCorvusProgramImage.g.cs`](two-stage-annotated-demo/generated/FlagshipCorvusProgramImage.g.cs) consumed by the isolated deployment project |
| Producer/harness code | [`CorvusImageDeployment.csproj`](two-stage-annotated-demo/deployment/CorvusImageDeployment.csproj), [`Program.cs`](two-stage-annotated-demo/deployment/Program.cs), and [`InspectManagedAssembly.cs`](two-stage-annotated-demo/deployment/InspectManagedAssembly.cs) |
| Exact command | Trimmed and NativeAOT publish/run plus `find`, `stat`, `.deps.json` grep, `System.Reflection.Metadata`, `strings`, and `nm` commands retained in the transcript |
| Retained output | [`deployment-inspection-current.txt`](two-stage-annotated-demo/results/deployment-inspection-current.txt) |
| Result mapping | Project properties establish trim/full and generated image input; `Program.cs` validates the valid/invalid pair; each transcript section maps publish, run, footprint, dependency, managed metadata/IL, and native inspection |

## Limitations

- The producer package is a locally built, unpublished bounded POC.
- The canonical-only package still carries unconditional JsonSchema.Net-related
  package closure; a package split remains external work.
- One conditional multi-resource model does not establish coverage for every
  annotation, vocabulary, reference graph, engine, OS, or architecture.
- OpenAPI 3.0 projection is intentionally conservative and lossy.
- The image contains no regex patterns, and larger graph scaling remains
  unmeasured.
- JsonSchema.Net and Corvus licensing/approval remain application concerns.

## Reproduction and deeper evidence

- [Hands-on demo and meaning of `verified`](two-stage-annotated-demo/README.md)
- [Generated allocation interpretation](allocation-results.md)
- [Detailed technical record](same-pass-exporter-removal.md)
- [Raw same-pass BDN results](two-stage-annotated-demo/results/same-pass/)
- [Raw symmetric-binding BDN results](two-stage-annotated-demo/results/symmetric-bindings/)
- [Cold-process CSV](two-stage-annotated-demo/results/same-pass/cold-process.csv)
- [Generated-artifact archive](archive/README.md)
