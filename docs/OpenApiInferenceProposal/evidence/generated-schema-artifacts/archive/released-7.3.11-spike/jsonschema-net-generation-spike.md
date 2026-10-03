# Historical: JsonSchema.Net.Generation 7.3.11 interoperability spike

> **Archived phase-1 evidence (2026-09-25).** This document records the released
> 7.3.11 generator investigation, the original generator-ordering blocker, native
> graph measurements, and the first Corvus program-image proof. Its identities and
> benchmark artifacts remain authoritative only for that historical model and
> resource graph. The current result is the
> [same-pass exporter-removal proof](../../same-pass-exporter-removal.md), with a
> [hands-on demo](../../two-stage-annotated-demo/README.md).

## Historical result before the upstream prototype

The flagship shipping-quality dual-validator binding cannot be implemented with the current
`JsonSchema.Net.Generation` public/generated surface without duplicating its annotation semantics
or performing schema construction and serialization at runtime. The spike therefore stops at the
approved upstream-generator boundary. A non-shipping runtime bundle was nevertheless measured to
quantify the native path and the cost that an upstream artifact would remove.

The inspection uses `JsonSchema.Net.Generation` 7.3.11, whose package records source commit
`ff430467e33e54d56537954fae73dbdda2c95246` and depends on `JsonSchema.Net` 9.4.0. The package
requires acceptance of the Open Source Maintenance Fee EULA. The existing Corvus evidence uses
`Corvus.Text.Json.Validator` 5.6.1 under Apache-2.0. The program-image follow-up builds
`Corvus.Text.Json` from authoritative commit
`6af6c149ee5c9461faa9850a34d0e0cff1fd9be2`; those APIs are newer than the released 5.6.1
package used by the original adapter evidence.

`jsonschema-net-generation-inspection` builds a representative annotated model with object,
scalar, conditional, nested-resource, required-property, and closed-object annotations. The
unmodified upstream generator emits this shape:

```csharp
public static partial class GeneratedJsonSchemas
{
    public static readonly JsonSchema FlagshipModel = new JsonSchemaBuilder()
        .Schema("https://json-schema.org/draft/2020-12/schema")
        .Id("urn:jsonschema:GeneratedSchemaInspection.FlagshipModel")
        .Type(SchemaValueType.Object)
        // Properties, required, if/then, and unevaluated/additional properties...
        .Build();

    public static readonly JsonSchema FlagshipAddress = new JsonSchemaBuilder()
        .Schema("https://json-schema.org/draft/2020-12/schema")
        .Id("urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress")
        // Address shape...
        .Build();
}
```

The root schema refers to the address with
`$ref: "urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress"`, and generated module
initialization registers both runtime `JsonSchema` objects with JsonSchema.Net.

## Why an external adapter generator could not consume released 7.3.11

1. Roslyn generators run against the same input compilation and cannot observe symbols or values
   emitted by another generator in that compilation. An adapter generator cannot see
   `GeneratedJsonSchemas.FlagshipModel`.
2. In a downstream compilation, the field symbol is visible but its `JsonSchema` value and
   `JsonSchemaBuilder` initializer are not compile-time constants. Roslyn exposes neither exact
   canonical schema bytes nor the constructed keyword graph.
3. The generated graph is a resource set, not one self-contained schema. Nested CLR types are
   emitted as separate schemas connected by absolute URN references. The ASP.NET artifact ABI
   intentionally accepts exact, self-contained source bytes with prevalidated local references;
   importing only the root would reject or lose the address contract.
4. Accessing the field and serializing it in a static initializer would execute schema builders,
   construct registries/dictionaries, serialize, normalize, and hash at application startup. That
   is runtime reflection/object construction, not ahead-of-time artifact generation, and would
   make the JsonSchema.Net and Corvus bindings depend on potentially different byte authority.
5. Copying Greg's attributes into a second semantic generator would create two implementations of
   naming, nullability, condition groups, custom handlers, references, and future annotation
   behavior. The spike explicitly rejects that approach.

Consequently, the same exact generated source identity cannot yet be proven across the
JsonSchema.Net and Corvus bindings, and dual-engine correctness/performance measurements would be
measuring a hand-copied schema rather than the upstream generated artifact.

## Measured native graph

The inspection project uses BenchmarkDotNet 0.13.0 with `MemoryDiagnoser`, setup outside benchmark
methods, and no manual invocation loops. Dry and Short runs passed before the default run. The
default report is
[`GeneratedSchemaInspection.GenerationBenchmarks-report-github.md`](results/final/20260925-130626/GeneratedSchemaInspection.GenerationBenchmarks-report-github.md).

Environment: Ubuntu 22.04 under WSL, 13th Gen Intel Core i7-13800H (10 physical/20 logical
cores), .NET SDK 11.0.100-rc.1.26420.103, and .NET 11.0.0 x64 RyuJIT. These are comparative
machine-local results, not cross-machine targets.

| Operation | Mean | Allocated |
|---|---:|---:|
| Warm generated static-field access | 0.84 ns | 0 B |
| Rebuild equivalent two-resource `JsonSchemaBuilder` graph | 38.03 us | 58,720 B |
| Serialize generated root only | 669.82 ns | 624 B |
| Copy evidence-only canonical byte prototype | 67.68 ns | 936 B |
| `SchemaRegistry.CreateBundle` over both generated resources | 36.47 us | 63,216 B |
| Parse the supported complete bundle | 32.75 us | 58,496 B |
| Repeated Corvus `FromText` after its process cache is populated | 414.34 ns | 3,472 B |
| JsonSchema.Net generated-root valid evaluation | 16.79 us | 19,641 B |
| JsonSchema.Net generated-root invalid evaluation | 19.56 us | 25,458 B |
| JsonSchema.Net parsed-bundle valid evaluation | 17.25 us | 24,818 B |
| JsonSchema.Net parsed-bundle invalid evaluation | 19.95 us | 32,579 B |
| Corvus bundle valid evaluation | 245.00 ns | 168 B |
| Corvus bundle invalid evaluation | 288.41 ns | 168 B |

The native generated object is therefore the correct JsonSchema.Net fast path: warm access itself
is allocation-free, it avoids roughly 33 us/58 KB of bundle parsing, and its evaluation allocates
about 5.2 KB less for this valid payload than evaluation through the reparsed bundle. Serialization
and reparsing should not be inserted into that binding.

The valid/invalid corpus is asserted in `GlobalSetup` against the generated root, the supported
bundle reparsed by JsonSchema.Net, and Corvus compiled from the same 1,075-byte supported bundle.
All three agree. The bundle SHA-256 is
`CF865FB222DA8D57CD82C5146A2EB52052370407C3C63FF2362F802B3D2F4B72`.
The root-only serialization is 556 bytes with SHA-256
`DDF7EDA73CB17B57B47089C03C8C7CADCBCABD82D4E869CBCF67BC3373A5D055`,
but a clean process evaluating it fails with
`RefResolutionException: Could not resolve
'urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress'`. Root serialization is not an
engine-neutral artifact.

The evidence-only canonical byte prototype rewrites the nested absolute reference into local
`$defs`. It demonstrates the desirable generated output shape and cheap byte access, but it is
hand-authored from the inspected builder semantics. It is not proven to be an upstream artifact
and is not used to claim identity equivalence.

## Cold and one-time costs

A paired repeated-process harness alternated 30 baseline and 30 generated launches. The baseline
has the same BenchmarkDotNet, Corvus, and JsonSchema.Net.Generation package references but no
annotated models. It therefore excludes ordinary host/package closure startup as far as a separate
executable permits.

| Process measurement | Median | Mean | Standard deviation |
|---|---:|---:|---:|
| Matched baseline | 113.78 ms | 114.50 ms | 9.18 ms |
| Generated model executable | 174.64 ms | 172.05 ms | 14.95 ms |
| Paired generated-minus-baseline delta | 56.90 ms | 57.55 ms | 11.52 ms |

The delta includes generated module initialization, both builder graphs, validating-converter
registration, and differences in generated assembly loading/JIT. It cannot isolate those
pre-`Main` contributors or reliably measure their managed allocation. Once initialization has
completed, BDN measures static field access at 0.84 ns/0 B.

The released 5.6.1 Corvus wrapper's compilation was measured separately in 30 fresh processes, with bundle construction outside
the timer and `GC.GetAllocatedBytesForCurrentThread` around the first `FromText` call. Median was
87.90 ms, mean 92.39 ms, standard deviation 16.43 ms, and every run allocated 91,312 B on the
calling thread. The 414 ns BDN row is intentionally the repeated cached path and must not be
mistaken for first compilation. This is the runtime fallback, not the recommended generated path.

## Corvus precompiled program image

The non-shipping `corvus-image-producer` consumes the same canonical 1,075-byte complete resource
bundle at evidence-build time. With explicit Draft 2020-12, format-annotation, content-assertion,
interpreted-regex, and depth options, it calls `JsonSchemaEvaluator.Compile`, registers the
generated model entry point, and emits `ToProgramImage()` as a closed binding-private byte payload.
No schema text, document resolver, loader, or compiler runs when the generated binding calls
`FromProgramImage`.

| Artifact property | Value |
|---|---|
| Canonical schema graph identity | `CF865FB222DA8D57CD82C5146A2EB52052370407C3C63FF2362F802B3D2F4B72` |
| Canonical bundle size | 1,075 B |
| Corvus source/package identity | commit `6af6c149ee5c9461faa9850a34d0e0cff1fd9be2` |
| Program-image format | version 6 |
| Program-image identity | `CA051957A5007AB66DC5BA7444A6DCE870747024075C0A101F549E2E67FF402C` |
| Program-image size | 1,048 B |
| Configuration identity | `draft2020-12;format=false;content=true;regex=interpreted;max-depth=128` |
| Composite binding identity | `B2782B2D33BC20BB4ADBFC2D5515943963AF4AEFC989BD04D15C9E09BDF32901` |
| Image regex patterns | 0 |

The image is 27 B smaller than the canonical graph. Its identity is validator-specific and does
not replace the graph identity. The binding identity hashes the graph identity, exact Corvus
commit, image version, and configuration identity. The evidence binding checks graph/configuration
compatibility before loading and uses one `Lazy<JsonSchemaEvaluator>` with execution-and-publication
thread safety for its process-lifetime singleton.

The final `MemoryDiagnoser` report is
[`GeneratedSchemaInspection.ProgramImageBenchmarks-report-github.md`](results/image-final2/20260925-141304/GeneratedSchemaInspection.ProgramImageBenchmarks-report-github.md).
Dry and Short passed first.

| Operation | Mean | Allocated |
|---|---:|---:|
| Compile canonical bundle | 30.51 us | 63,027 B |
| Load version-6 program image | 10.68 us | 45,752 B |
| Compiled evaluator, valid / invalid | 343.3 / 323.2 ns | 168 / 168 B |
| Image evaluator, valid / invalid | 318.0 / 295.0 ns | 168 / 168 B |
| JsonSchema.Net native graph, valid / invalid | 83.51 / 93.59 us | 19,642 / 25,460 B |

Image load is 2.86 times faster and allocates 17,275 B less than compilation in the default run.
The compiled and image evaluators have the same steady-state allocation and statistically
equivalent throughput. JsonSchema.Net's native rows were unusually noisy and much slower than the
earlier default run on the same machine; they preserve the requested side-by-side observation but
should not be compared across runs as a package-performance claim.

A paired, alternating 30-process harness measured first compile and first image load under a
heavily loaded machine. Compile was 602.57 ms median (602.79 ms mean, 87.78 ms standard deviation)
and 90,224 B. Image load was 304.50 ms median (311.94 ms mean, 42.92 ms standard deviation) and
73,480 B. The paired saving was 291.09 ms median and exactly 16,744 B. Absolute process-cold time
includes runtime/assembly JIT and varied substantially; the paired direction and allocation delta
are the useful observations.

Correctness evidence proves:

- compiler, image evaluator, and JsonSchema.Net native graph agree on the identical corpus;
- compiler-to-image round trip is byte-for-byte identical to the embedded image;
- pattern metadata matches the image;
- corrupt image version and mismatched validator configuration are rejected;
- eight concurrent accesses publish exactly one lazy binding evaluator.

The flagship schema needs no regex-backed pattern, so `GetImagePatterns()` returns zero. A
`[GeneratedRegex]` provider would add no behavior or cost here and is deliberately not claimed.
The producer records the empty pattern table. A pattern-bearing corpus remains necessary before
quantifying generated-regex startup savings.

## Successor: same-pass annotated-model proof

The follow-up [same-pass exporter-removal proof](../../same-pass-exporter-removal.md) and
[annotated-model demo](../../two-stage-annotated-demo/README.md) replace the
active trusted exporter with a locally packed 7.3.11 upstream prototype. The same annotation pass
now emits a deterministic 1,803-byte canonical resource bundle and ordered manifest with graph
identity `5F26E1108737429022275068F71A3F513D04A26E071E8EBECCE88B76CFF9A83A`.
Those exact generated facts feed the ASP.NET artifact generator and a 726-byte Corvus program image.
An independent `NativeAndCanonical` build from the same model/package preserves the native
JsonSchema.Net comparison. Native and parsed graphs, Corvus compile/image, Minimal API, MVC, and
OpenAPI 3.0/3.1/3.2 agree on one corpus and identity.

The collectible-ALC exporter remains historical benchmark evidence only. The smallest upstream
follow-up has narrowed from artifact emission to packaging: canonical-only consumers need analyzer
and canonical support without unconditional JsonSchema.Net/Json.More/JsonPointer/Humanizer runtime
closure.

## Recommended native-first integration

JsonSchema.Net should retain a private validator-specific handle to the generated `JsonSchema`
root plus its generated resource registry. The framework artifact remains engine-neutral and
authoritative: ordered resources, root URI, dialect, canonical UTF-8 per resource (or one
deterministic supported bundle), and hashes generated from the same upstream semantic pass.
The Corvus producer consumes those bytes at build time and emits a version/configuration-bound
program image; JsonSchema.Net consumes its native handle. Neither engine parses or compiles the
schema at application startup. ASP.NET Core need not reference either engine because each closed
validator binding owns its private native handle or image.

Identity should cover the complete ordered resource graph, not one incidental root serialization.
A suitable manifest hashes each canonical resource and then hashes the ordered tuples
`(resource URI, dialect, resource hash)` plus the root URI. This Merkle-like identity makes graph
membership and ordering explicit, avoids dependence on global registry state, and lets the
upstream generator prove that native builders and emitted bytes came from one semantic model.

The current module initializer has process-global converter registration and generated builders
whose `$id` values register into JsonSchema.Net's global registry. Rebuilding identical IDs against
that registry throws rather than overwrites. Generated applications should prefer an immutable
per-artifact resource registry/native handle, reserving global registration for opt-in serializer
integration. This also narrows collision and concurrency concerns between independently generated
resource sets.

## Historical upstream options considered

The same-pass prototype subsequently implemented the manifest/artifact direction
described below. These options are retained to show how the evidence narrowed the
upstream request; they are not current missing prerequisites.

### 1. Emit a stable generated artifact type (recommended)

For every generated root, emit a closed artifact beside the existing `JsonSchema` field:

```csharp
public sealed class FlagshipModelSchemaArtifact
{
    public static ReadOnlyMemory<byte> SourceSchema { get; }
    public static ReadOnlyMemory<byte> Resources { get; }
    public static string Dialect { get; }
    public static string SourceIdentity { get; }
}
```

`SourceSchema` must be canonical exact UTF-8 produced by the same upstream semantic model.
`Resources` must deterministically bundle secondary generated schemas, or the root must rewrite
them into local `$defs` references. The identity must cover the exact root and bundled resource
bytes. The type should remain ecosystem-neutral; a generic ASP.NET adapter can consume it without
the upstream package referencing ASP.NET Core. This is the smallest surface that supports two
independent validators and exact OpenAPI identity without generator ordering.

### 2. Emit a deterministic schema-resource manifest

Add an opt-in upstream MSBuild/source-generator output containing root bytes, all referenced
resource bytes, dialect, logical CLR type, and hashes. A downstream project—not a same-compilation
generator—can consume that manifest as an `AdditionalFile` and emit the ASP.NET artifact/bindings.
This naturally establishes a two-project producer/consumer boundary, but requires build-target
plumbing and incremental-output ownership.

### 3. Add a build-time export tool backed by upstream semantics

Ship an upstream-supported tool that consumes the generator's own semantic model or compiled
producer assembly and writes the manifest above. Executing the compiled assembly is less desirable
because it runs arbitrary module initializers and generated builders, but it can be bounded to a
separate build step. An external reflection-based exporter implemented only in this repository
would be fragile and is not recommended as evidence of a stable integration.

## Historical work plan after the upstream hook

The successor proof completed this work. The list remains as the acceptance criteria
that the same-pass prototype was measured against.

Once option 1 or 2 exists, the same artifact can be paired with:

- a JsonSchema.Net binding using Draft 2020-12 and explicit
  `RequireFormatValidation` capability;
- a Corvus binding using the same exact bytes/dialect and corresponding
  `AlwaysAssertFormat` setting.

The artifact source identity and OAS 3.0/3.1/3.2 output must be byte/model-identical, while each
binding's composite identity includes its distinct validator configuration. Both engines can then
run the same valid/invalid corpus and side-by-side startup, throughput, and allocation benchmarks
without hiding semantic differences.
