# Public API overview

The proposal is split into six independently reviewable groups. Later groups reuse named concepts
from earlier groups but do not force approval of unrelated capabilities. Every prototype API is
experimental under `ASP0040`.

The exhaustive prototype inventory is archived at
[Full API history](archive/2026-10-prototype-design-history/api-surface.md).

## Review roadmap

| Review | Purpose | Principal APIs | Dependency | Evidence |
| --- | --- | --- | --- | --- |
| 1 | Opt-in inference and scalar-format policy | `OpenApiSchemaGenerationMode`, `OpenApiScalarFormatPolicy`, `OpenApiScalarFormatContext` | None | [Core inference](evidence/core-inference-details.md#focused-comparisons-and-practical-impact) |
| 2 | Version-targeted generation and transformers | `IOpenApiVersionedDocumentProvider`, `OpenApiVersion` on transformer contexts | None | [Cross-version artifacts](evidence/core-inference-details.md#artifacts-and-normalization) |
| 3 | Positional tuple JSON | `JsonArrayTupleConverter`, `JsonArrayTupleConverters` | None; inference recognizes it when both are selected | [Tuple/provider tests](evidence/schema-evidence-providers.md#what-is-covered) |
| 4 | Narrow runtime-enforced evidence | `IOpenApiSchemaEvidenceProvider`, `OpenApiSchemaEvidence`, `OpenApiSchemaNumber` | Review 1 inference | [Provider safety and exact numbers](evidence/schema-evidence-providers.md) |
| 5 | Dynamic complete-schema endpoint enforcement | Runtime evidence, validator factory/validator, registration/options | Review 4 directional purpose | [Runtime adapter and endpoint policy](evidence/validated-schema-adapters/README.md) |
| 6 | Generated complete-schema artifacts | Artifact, static validator, binding, generic endpoint extensions | Review 5 dialect/result/options concepts | [Generated authority and bindings](evidence/generated-schema-artifacts/current-proof.md) |

## Reviews 1–3: inference, target version, and tuples

Review 1 adds `Legacy`/`Inferred` generation modes and policy-controlled scalar formats. The
framework creates `OpenApiScalarFormatContext`; applications can preserve conventional formats,
emit only formats compatible with the full accepted language, suppress optional formats, or use a
contextual callback.

Review 2 adds target-version document generation and exposes the selected `OpenApiSpecVersion` to
schema, operation, and document transformers.

Review 3 supplies opt-in positional JSON converters for `Tuple` and `ValueTuple`, including closed
converter factories for NativeAOT. Converter registration changes runtime JSON; inference merely
recognizes that package-owned contract.

These groups are independently useful. Version-aware transformer contexts do not require inferred
mode. Tuple converters do not activate merely because the package or OpenAPI services are present.
Inference remains opt-in and Legacy remains the default.

## Review 4: narrow evidence

```csharp
public interface IOpenApiSchemaEvidenceProvider
{
    OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context);
}

public abstract class OpenApiSchemaEvidence;

public sealed class OpenApiScalarSchemaEvidence : OpenApiSchemaEvidence
{
    public OpenApiScalarSchemaEvidence(
        OpenApiScalarSchemaValueKind valueKind,
        string? format = null,
        string? pattern = null,
        OpenApiSchemaNumber? minimum = null,
        OpenApiSchemaNumber? maximum = null);
}
```

`OpenApiSchemaNumber` is an immutable exact JSON number represented by normalized
`BigInteger significand × 10^exponent`. It supports invariant parsing, formatting, equality, and
bounded comparison without expanding enormous powers of ten. This permits fractional and
scientific bounds without decimal/double precision loss.

Evidence carries only strict scalar and fixed positional-array facts. It neither models complete
JSON Schema nor installs endpoint validation.

`OpenApiSchemaEvidenceContext` is framework-created and includes declared/effective type,
`JsonTypeInfo`, converter, and directional purpose. This ties every provider claim to the runtime
mechanism and request/response direction that proves it.

## Review 5: runtime complete-schema validation

```csharp
public interface IOpenApiJsonSchemaValidatorFactory
{
    string ConfigurationIdentity { get; }
    bool SupportsDialect(OpenApiJsonSchemaDialect dialect);
    IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence);
}

public interface IOpenApiJsonSchemaValidator
{
    ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiJsonSchemaValidationContext context,
        CancellationToken cancellationToken = default);
}

public sealed class OpenApiValidatedJsonSchemaRegistration
{
    public OpenApiValidatedJsonSchemaRegistration(
        Type type,
        OpenApiSchemaEvidencePurpose purpose,
        ReadOnlyMemory<byte> utf8Schema,
        OpenApiJsonSchemaDialect dialect,
        OpenApiJsonSchemaValidationCapabilities validationCapabilities,
        IOpenApiJsonSchemaValidatorFactory validatorFactory,
        OpenApiValidatedJsonSchemaOptions? options = null);
}

public sealed class OpenApiValidatedJsonSchemaOptions
{
    public long MaxPayloadSize { get; init; }
    public bool AllowEmptyRequestBody { get; init; }
    public int? ResponseStatusCode { get; init; }
    public string ContentType { get; init; }
}

public static TBuilder WithValidatedJsonSchema<TBuilder>(
    this TBuilder builder,
    OpenApiValidatedJsonSchemaRegistration registration)
    where TBuilder : IEndpointConventionBuilder;
```

The factory is the one-time adapter/compile seam. The reusable validator is the per-payload
execution seam. `OpenApiJsonSchemaValidationResult` and `OpenApiJsonSchemaValidationError` prevent
native engine objects from entering framework policy.

`SchemaIdentity` belongs to source authority; `ConfigurationIdentity` belongs to engine semantics;
the registration's composite `Identity` prevents cache collisions. Options control maximum
payload size, empty requests, output status, and content type.

The runtime schema authority is exposed as `OpenApiValidatedJsonSchemaEvidence`, which retains
source schema, dialect, capabilities, schema identity, validator configuration identity, and
composite identity. Despite the historical class name, this is complete validated-schema
authority, not the narrow provider algebra.

The registration overload preserves the concrete Minimal API builder type. A controller overload
accepts `Func<ControllerActionDescriptor, bool>` and selects actions during endpoint construction.

## Review 6: generated/AOT complete-schema validation

```csharp
public interface IOpenApiValidatedJsonSchemaArtifact<TSelf>
    where TSelf : IOpenApiValidatedJsonSchemaArtifact<TSelf>
{
    static abstract ReadOnlyMemory<byte> SourceSchema { get; }
    static abstract ReadOnlyMemory<byte> NormalizedSchema { get; }
    static abstract ReadOnlyMemory<byte> LocalReferences { get; }
    static abstract OpenApiJsonSchemaDialect Dialect { get; }
    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }
    static abstract string SchemaIdentity { get; }
    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);
}

public interface IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
    where TArtifact : IOpenApiValidatedJsonSchemaArtifact<TArtifact>
    where TSelf : IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
{
    static abstract string ConfigurationIdentity { get; }
    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default);
}

public interface IOpenApiValidatedJsonSchemaBinding<TSelf>
    where TSelf : IOpenApiValidatedJsonSchemaBinding<TSelf>
{
    static abstract Type Type { get; }
    static abstract string SchemaIdentity { get; }
    static abstract string Identity { get; }
    static abstract OpenApiJsonSchemaDialect Dialect { get; }
    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }
    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);
    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> utf8Json,
        OpenApiSchemaEvidencePurpose purpose,
        CancellationToken cancellationToken = default);
}

public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
    this IEndpointConventionBuilder builder,
    OpenApiSchemaEvidencePurpose purpose)
    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;

public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
    this IEndpointConventionBuilder builder,
    OpenApiSchemaEvidencePurpose purpose,
    OpenApiValidatedJsonSchemaOptions? options)
    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;
```

The artifact is portable authority, the validator is private execution, and the binding is their
closed endpoint-facing composition. ASP.NET type-erases the binding once at startup. Generated
paths do not parse, normalize, hash, resolve, or compile schemas while building endpoints or
processing requests.

Runtime and generated paths converge on the same internal registration and endpoint plan. See
[Architecture](architecture.md#validator-seams) for ownership and lifecycle.

Generated endpoint overloads accept direction, optional `OpenApiValidatedJsonSchemaOptions`, and
an optional controller action predicate. They return `IEndpointConventionBuilder` because only
`TBinding` is supplied; generated convenience extensions can preserve concrete builder types.

## Selection summary

| Need | Review |
| --- | --- |
| Better inferred OpenAPI | 1 |
| Target-aware documents or transformers | 2 |
| Tuple-as-array runtime JSON | 3 |
| Narrow facts from an existing converter/parser | 4 |
| Dynamic complete schema plus endpoint validation | 5 |
| Build-time complete schema plus generated/AOT validation | 6 |

See [Design](design.md) for product behavior and
[Integrations](integrations.md) for replaceable external examples.
