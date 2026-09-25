# Validated JSON Schema adapter evidence

This non-shipping Minimal API evidence project exercises two adapters over immutable Draft 4, Draft 6, Draft 7, Draft 2019-09, and Draft 2020-12 schemas, an equivalent valid/invalid recursive payload corpus for request and response purposes, semantic schema identity, and concurrent reuse. Both adapters compile/build the schema once in `CreateValidator`; request validation reuses the compiled validator and never fetches HTTP or filesystem references.

- `Corvus.Text.Json.Validator` 5.6.1 is Apache-2.0. The adapter explicitly selects Draft 4, 6, 7, 2019-09, or 2020-12, uses `JsonSchema.FromText`, the collector-free raw UTF-8 `Validate` fast path for success, and `AlwaysAssertFormat` when the framework capability requests format assertions. Immutable framework evidence rejects non-local and unresolved references before the adapter compiles the schema.
- `JsonSchema.Net` 9.4.0 uses a private `SchemaRegistry`, explicitly selects Draft 6, 7, 2019-09, or 2020-12, calls `Evaluate(JsonElement)`, and honors `RequireFormatValidation`. It does not implement Draft 4, which registration rejects before compilation. No remote `Fetch` callback is installed. The source is MIT; the published binary package is governed by the Open Source Maintenance Fee EULA. Use of this evidence package was explicitly approved for this prototype and consumers must independently accept the applicable package terms.

The shipping `Microsoft.AspNetCore.OpenApi` project references neither package. The framework-facing errors are deliberately engine-neutral and client-safe.

The executable also runs the real endpoint convention wrapper with each engine. It proves valid request/response pass-through, invalid-request 400, invalid-response suppression/500, request and response size limits, and annotation-only versus asserted `format` behavior.

Each adapter caches compiled validators by semantic evidence identity. That identity covers the exact immutable source schema bytes, declared dialect, vocabulary/format capabilities, and stable adapter configuration identity, so validators with different semantics cannot collide. A repository-local trimmed publish currently cannot isolate this evidence app: `PublishTrimmed` propagates into source-generator and `net462` projects in the ASP.NET Core source reference graph and fails with `NETSDK1124` before adapter analysis. The shipping OpenAPI trimming fixture remains the applicable product check; adapter NativeAOT compatibility is therefore not claimed.

Allocation measurements and their explicit framework/engine boundary are recorded in [`allocation-results.md`](allocation-results.md).

Run from the repository root after activating the repository SDK:

```console
dotnet run --project docs/OpenApiInferenceProposal/evidence/validated-schema-adapters/ValidatedSchemaAdapters.csproj
```
