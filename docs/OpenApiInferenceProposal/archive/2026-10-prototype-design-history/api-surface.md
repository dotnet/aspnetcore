# Experimental OpenAPI inference: proposed API surface

This appendix inventories the complete public API added by the prototype relative to merge base `89ab93803f3fcbb928f8ed1523945f89284f7e79`.

The APIs should not be reviewed as one indivisible block. The six reviews below appear in
dependency order and remain separately decidable. All prototype APIs use the existing experimental
diagnostic `ASP0040`.

## Review roadmap

| Review | Purpose | Dependencies |
| --- | --- | --- |
| 1. Inferred schemas and scalar formats | Opt-in serializer/binder-faithful inference and scalar-format policy | None |
| 2. Version-targeted generation and transformers | Make the target OpenAPI version explicit during generation and transformation | None; review 1 uses it when both are selected |
| 3. Positional tuple JSON | Opt-in tuple converters with closed NativeAOT registration | None; review 1 recognizes the converter contract when both are selected |
| 4. Runtime-enforced schema evidence | Add narrow facts from recognized converters/parsers to ordinary inference | Review 1 inference; it also defines the shared directional-purpose concept |
| 5. Endpoint-enforced validated JSON Schema | Bind a complete schema and runtime validator to request/response enforcement | Review 4's directional-purpose type; otherwise independent of evidence providers and inferred generation |
| 6. Generated validated-schema artifacts | Provide the static artifact/validator/binding ABI for pre-generated schemas | Review 5's dialect, result, options, and endpoint-enforcement concepts |

## Validator seam map

API review 5 is the runtime path: `IOpenApiJsonSchemaValidatorFactory` checks the dialect and
creates one reusable `IOpenApiJsonSchemaValidator` for
`OpenApiValidatedJsonSchemaRegistration`. API review 6 is the generated path:
`IOpenApiValidatedJsonSchemaArtifact<TSelf>` carries portable schema authority,
`IOpenApiValidatedJsonSchemaValidator<TArtifact,TSelf>` supplies static engine execution, and
`IOpenApiValidatedJsonSchemaBinding<TSelf>` closes the CLR type, artifact, validator, and composite
identity. Both paths converge internally on the same type-erased registration and endpoint plan;
see [Validator seams](architecture.md#validator-seams) for ownership and lifecycle.

## API review 1: inferred schemas and scalar formats

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiSchemaGenerationMode
+{
+    Legacy = 0,
+    Inferred = 1,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiScalarFormatPolicy
+{
+    Conventional = 0,
+    CompatibleOnly = 1,
+    None = 2,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiScalarFormatLocation
+{
+    JsonBody = 0,
+    JsonProperty = 1,
+    Route = 2,
+    Query = 3,
+    Header = 4,
+    Form = 5,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiScalarFormatPurpose
+{
+    Neutral = 0,
+    Input = 1,
+    Output = 2,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiScalarFormatProvenance
+{
+    Unknown = 0,
+    SystemTextJsonBuiltIn = 1,
+    FrameworkBuiltInParser = 2,
+    CustomConverter = 3,
+    CustomParser = 4,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public sealed class OpenApiScalarFormatContext
+{
+    public Type Type { get; }
+    public Type EffectiveType { get; }
+    public OpenApiScalarFormatLocation Location { get; }
+    public OpenApiScalarFormatPurpose Purpose { get; }
+    public OpenApiSpecVersion OpenApiVersion { get; }
+    public OpenApiScalarFormatProvenance Provenance { get; }
+    public string? DefaultFormat { get; }
+}
+
 public sealed class OpenApiOptions
 {
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiSchemaGenerationMode SchemaGenerationMode { get; set; }
+
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiScalarFormatPolicy ScalarFormatPolicy { get; set; }
+
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public Func<OpenApiScalarFormatContext, string?>? CreateScalarFormat { get; set; }
 }
```

`OpenApiScalarFormatContext` has an internal constructor and is a framework-supplied input only. Legacy is the default schema-generation mode. Conventional is the default inferred scalar-format policy.

## API review 2: version-targeted generation and transformers

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public interface IOpenApiVersionedDocumentProvider : IOpenApiDocumentProvider
+{
+    Task<OpenApiDocument> GetOpenApiDocumentForVersionAsync(
+        OpenApiSpecVersion openApiVersion,
+        CancellationToken cancellationToken = default);
+}

 public sealed class OpenApiDocumentTransformerContext
 {
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiSpecVersion OpenApiVersion { get; init; }
 }

 public sealed class OpenApiOperationTransformerContext
 {
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiSpecVersion OpenApiVersion { get; init; }
 }

 public sealed class OpenApiSchemaTransformerContext
 {
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiSpecVersion OpenApiVersion { get; init; }
 }
```

The existing `IOpenApiDocumentProvider` surface is unchanged. The built-in provider implements and is keyed-registered as the derived interface.

## API review 3: positional tuple JSON

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public sealed class JsonArrayTupleConverter : JsonConverterFactory
+{
+    [RequiresDynamicCode(...)]
+    [RequiresUnreferencedCode(...)]
+    public JsonArrayTupleConverter();
+
+    public override bool CanConvert(Type typeToConvert);
+
+    [RequiresDynamicCode(...)]
+    [RequiresUnreferencedCode(...)]
+    public override JsonConverter CreateConverter(
+        Type typeToConvert,
+        JsonSerializerOptions options);
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public static class JsonArrayTupleConverters
+{
+    public static JsonConverter<ValueTuple> CreateValueTuple();
+    public static JsonConverter<ValueTuple<T1>> CreateValueTuple<T1>();
+    public static JsonConverter<(T1, T2)> CreateValueTuple<T1, T2>();
+    public static JsonConverter<(T1, T2, T3)> CreateValueTuple<T1, T2, T3>();
+    public static JsonConverter<(T1, T2, T3, T4)> CreateValueTuple<T1, T2, T3, T4>();
+    public static JsonConverter<(T1, T2, T3, T4, T5)> CreateValueTuple<T1, T2, T3, T4, T5>();
+    public static JsonConverter<(T1, T2, T3, T4, T5, T6)> CreateValueTuple<T1, T2, T3, T4, T5, T6>();
+    public static JsonConverter<(T1, T2, T3, T4, T5, T6, T7)> CreateValueTuple<T1, T2, T3, T4, T5, T6, T7>();
+    public static JsonConverter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
+        CreateValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
+            JsonConverter<TRest> restConverter)
+        where TRest : struct, ITuple;
+
+    public static JsonConverter<Tuple<T1>> CreateTuple<T1>();
+    public static JsonConverter<Tuple<T1, T2>> CreateTuple<T1, T2>();
+    public static JsonConverter<Tuple<T1, T2, T3>> CreateTuple<T1, T2, T3>();
+    public static JsonConverter<Tuple<T1, T2, T3, T4>> CreateTuple<T1, T2, T3, T4>();
+    public static JsonConverter<Tuple<T1, T2, T3, T4, T5>> CreateTuple<T1, T2, T3, T4, T5>();
+    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6>> CreateTuple<T1, T2, T3, T4, T5, T6>();
+    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6, T7>> CreateTuple<T1, T2, T3, T4, T5, T6, T7>();
+    public static JsonConverter<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
+        CreateTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
+            JsonConverter<TRest> restConverter)
+        where TRest : ITuple;
+}
```

There is no separate public RDG switch. Applications opt into positional tuple JSON through existing HTTP JSON configuration:

```csharp
services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter()));
```

For NativeAOT, an application registers one or more closed converters:

```csharp
services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        JsonArrayTupleConverters.CreateValueTuple<long, bool>()));
```

RDG detects explicit package converter registration and adds generated closed converters only for additional statically discovered tuple contracts that are not already handled. Package presence, `AddOpenApi`, RDG, and inferred mode are inert without explicit converter registration.

## API review 4: runtime-enforced schema evidence

### Purpose and non-purpose

This API fills one narrow gap: an identifiable runtime converter or parser can enforce facts that
effective metadata cannot describe. A provider reports those already-enforced facts so ordinary
inference can produce a more faithful schema. It does not author a complete JSON Schema and does
not cause ASP.NET Core to validate the returned evidence.

The algebra is intentionally closed and version-independent. That keeps it composable with
existing inference and avoids introducing a second public schema DOM. Complete schemas and
endpoint enforcement belong to the validated-artifact APIs; documentation-only customization
belongs to transformers.

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiSchemaEvidencePurpose
+{
+    Neutral = 0,
+    Input = 1,
+    Output = 2,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public enum OpenApiScalarSchemaValueKind
+{
+    Boolean = 0,
+    String = 1,
+    Integer = 2,
+    Number = 3,
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public sealed class OpenApiSchemaEvidenceContext
+{
+    public Type Type { get; }
+    public Type EffectiveType { get; }
+    public JsonTypeInfo TypeInfo { get; }
+    public JsonConverter Converter { get; }
+    public OpenApiSchemaEvidencePurpose Purpose { get; }
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public interface IOpenApiSchemaEvidenceProvider
+{
+    OpenApiSchemaEvidence? GetSchemaEvidence(OpenApiSchemaEvidenceContext context);
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public abstract class OpenApiSchemaEvidence;
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public readonly struct OpenApiSchemaNumber :
+    IComparable<OpenApiSchemaNumber>,
+    IEquatable<OpenApiSchemaNumber>
+{
+    public OpenApiSchemaNumber(BigInteger significand, int exponent);
+
+    public BigInteger Significand { get; }
+    public int Exponent { get; }
+
+    public static OpenApiSchemaNumber Parse(string value);
+    public int CompareTo(OpenApiSchemaNumber other);
+    public bool Equals(OpenApiSchemaNumber other);
+    public override bool Equals(object? obj);
+    public override int GetHashCode();
+    public override string ToString();
+
+    public static implicit operator OpenApiSchemaNumber(int value);
+    public static implicit operator OpenApiSchemaNumber(long value);
+    public static implicit operator OpenApiSchemaNumber(BigInteger value);
+    public static implicit operator OpenApiSchemaNumber(decimal value);
+    public static bool operator ==(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+    public static bool operator !=(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+    public static bool operator <(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+    public static bool operator <=(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+    public static bool operator >(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+    public static bool operator >=(OpenApiSchemaNumber left, OpenApiSchemaNumber right);
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public sealed class OpenApiScalarSchemaEvidence : OpenApiSchemaEvidence
+{
+    public OpenApiScalarSchemaEvidence(
+        OpenApiScalarSchemaValueKind valueKind,
+        string? format = null,
+        string? pattern = null,
+        OpenApiSchemaNumber? minimum = null,
+        OpenApiSchemaNumber? maximum = null);
+
+    public OpenApiScalarSchemaValueKind ValueKind { get; }
+    public string? Format { get; }
+    public string? Pattern { get; }
+    public OpenApiSchemaNumber? Minimum { get; }
+    public OpenApiSchemaNumber? Maximum { get; }
+}
+
+[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+public sealed class OpenApiPositionalArraySchemaEvidence : OpenApiSchemaEvidence
+{
+    public OpenApiPositionalArraySchemaEvidence(IEnumerable<Type> elementTypes);
+    public IReadOnlyList<Type> ElementTypes { get; }
+}
+
 public sealed class OpenApiOptions
 {
+    [Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
+    public OpenApiOptions AddSchemaEvidenceProvider(IOpenApiSchemaEvidenceProvider provider);
 }
```

### Selection guidance

| Question | Answer |
| --- | --- |
| Is evidence a complete JSON Schema model? | No. It carries strict scalar and fixed positional-array facts into ordinary inference. |
| Does returning evidence make ASP.NET Core validate it? | No. The provider may report only behavior already enforced by its recognized runtime mechanism. |
| When should I use a transformer? | For documentation-only or application-owned semantics. Transformers add no runtime-enforcement claim. |
| When should I use a validated artifact? | When a complete canonical schema must drive both OpenAPI and endpoint enforcement. |
| Why not use an artifact for every converter? | Evidence reuses recursive inference and requires no schema document, validator, stable schema identity, or binding pipeline. |

`OpenApiSchemaEvidenceContext` has an internal constructor and is supplied by the framework. Its
declared/effective type, effective `JsonTypeInfo`, converter, and directional purpose let a
provider tie a claim to the runtime contract that proves it. The key invariant is that inferred
OpenAPI must not become stricter than runtime behavior.

Supported evidence consists of strict boolean, string, integer, and number shapes; enforced string
patterns; exact numeric bounds; policy-controlled format candidates; and fixed positional arrays.
Full objects, conditionals, composition, arbitrary schema import, and validator configuration are
outside this algebra. Those capabilities remain available through transformers or the separately
reviewable validated canonical-artifact tier, depending on whether runtime enforcement is needed.

JSON Schema permits any finite JSON number as an inclusive bound, including a fractional bound on
integer instances. `OpenApiSchemaNumber` therefore represents a normalized `BigInteger`
significand multiplied by `10` raised to a 32-bit exponent. This preserves values that `decimal`
or `double` would round, while avoiding mutable JSON DOM values and unvalidated strings in the
public contract. Zero is canonicalized and trailing decimal zeroes are removed, so equivalent
forms such as `1.2300` and `123e-2` compare and hash identically. Parsing accepts invariant JSON
number syntax only and caps input text and direct significands at 10,000 characters or digits.
Comparison uses decimal magnitude and digit-wise comparison rather than allocating an expanded
power of ten. Formatting emits exact invariant JSON-number text, using scientific notation when
expanded decimal notation would be disproportionate.

## API review 5: endpoint-enforced validated JSON Schema

This proposal is independent of inferred generation and the evidence-provider seam. It atomically binds exact immutable Draft 4, Draft 6, Draft 7, Draft 2019-09, or Draft 2020-12 bytes, a compiled validator with explicit dialect capabilities, directional endpoint metadata, and bounded request/response enforcement.

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+public enum OpenApiJsonSchemaDialect
+{
+    Draft202012 = 0,
+    Draft4 = 1,
+    Draft6 = 2,
+    Draft7 = 3,
+    Draft201909 = 4,
+}
+[Flags] public enum OpenApiJsonSchemaValidationCapabilities { None, FormatAssertions }
+public sealed class OpenApiValidatedJsonSchemaEvidence : OpenApiSchemaEvidence
+{
+    public JsonElement Schema { get; }
+    public OpenApiJsonSchemaDialect Dialect { get; }
+    public string Identity { get; }
+    public string SchemaIdentity { get; }
+    public OpenApiJsonSchemaValidationCapabilities ValidationCapabilities { get; }
+    public string ValidatorConfigurationIdentity { get; }
+}
+public sealed class OpenApiJsonSchemaValidationContext
+{
+    public OpenApiJsonSchemaValidationContext(
+        OpenApiValidatedJsonSchemaEvidence evidence,
+        OpenApiSchemaEvidencePurpose purpose);
+    public OpenApiValidatedJsonSchemaEvidence Evidence { get; }
+    public OpenApiSchemaEvidencePurpose Purpose { get; }
+}
+public sealed class OpenApiJsonSchemaValidationError
+{
+    public OpenApiJsonSchemaValidationError(string instanceLocation, string keyword, string message);
+    public string InstanceLocation { get; }
+    public string Keyword { get; }
+    public string Message { get; }
+}
+public readonly struct OpenApiJsonSchemaValidationResult
+{
+    public OpenApiJsonSchemaValidationResult();
+    public OpenApiJsonSchemaValidationResult(IEnumerable<OpenApiJsonSchemaValidationError> errors);
+    public static OpenApiJsonSchemaValidationResult Valid { get; }
+    public bool IsValid { get; }
+    public IReadOnlyList<OpenApiJsonSchemaValidationError>? Errors { get; }
+}
+public interface IOpenApiJsonSchemaValidator
+{
+    ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
+        ReadOnlyMemory<byte> utf8Json,
+        OpenApiJsonSchemaValidationContext context,
+        CancellationToken cancellationToken = default);
+}
+public interface IOpenApiJsonSchemaValidatorFactory
+{
+    string ConfigurationIdentity { get; }
+    bool SupportsDialect(OpenApiJsonSchemaDialect dialect);
+    IOpenApiJsonSchemaValidator CreateValidator(OpenApiValidatedJsonSchemaEvidence evidence);
+}
+public sealed class OpenApiValidatedJsonSchemaOptions
+{
+    public long MaxPayloadSize { get; init; }
+    public bool AllowEmptyRequestBody { get; init; }
+    public int? ResponseStatusCode { get; init; }
+    public string ContentType { get; init; }
+}
+public sealed class OpenApiValidatedJsonSchemaRegistration
+{
+    public OpenApiValidatedJsonSchemaRegistration(
+        Type type,
+        OpenApiSchemaEvidencePurpose purpose,
+        ReadOnlyMemory<byte> utf8Schema,
+        OpenApiJsonSchemaDialect dialect,
+        OpenApiJsonSchemaValidationCapabilities validationCapabilities,
+        IOpenApiJsonSchemaValidatorFactory validatorFactory,
+        OpenApiValidatedJsonSchemaOptions? options = null);
+    public Type Type { get; }
+    public OpenApiSchemaEvidencePurpose Purpose { get; }
+    public OpenApiValidatedJsonSchemaEvidence Evidence { get; }
+    public OpenApiValidatedJsonSchemaOptions Options { get; }
+}
+
 namespace Microsoft.AspNetCore.Builder;

+public static class OpenApiValidatedJsonSchemaEndpointConventionBuilderExtensions
+{
+    public static TBuilder WithValidatedJsonSchema<TBuilder>(
+        this TBuilder builder,
+        OpenApiValidatedJsonSchemaRegistration registration)
+        where TBuilder : IEndpointConventionBuilder;
+    public static TBuilder WithValidatedJsonSchema<TBuilder>(
+        this TBuilder builder,
+        OpenApiValidatedJsonSchemaRegistration registration,
+        Func<ControllerActionDescriptor, bool> actionPredicate)
+        where TBuilder : IEndpointConventionBuilder;
+}
```

Every declaration above carries `Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")`.

`SchemaIdentity` hashes exact source bytes. `Identity` additionally covers the declared dialect,
validation capabilities, and stable validator configuration identity, preventing caches from
colliding across different semantics. Factories must reject unsupported dialects before
compilation; no default dialect is inferred.

The result is a value type so `default`/`Valid` is the allocation-free success representation.
Error collections are created only on failure. Runtime registration calls the factory once to
create or compile a reusable validator; endpoint conventions then precompute directional
selectors and validation contexts while building the endpoint. The generated path performs no
factory compilation or schema processing during endpoint construction. The action-predicate
overload evaluates the predicate during controller endpoint construction and attaches the same
plan only to selected actions, allowing the same CLR type to use different schemas on different
actions. At runtime, bounded `ArrayPool<byte>` buffers and a pooled
`IHttpResponseBodyFeature` capture raw request/response bytes; synchronous successful validation
and copying avoid metadata, result, and buffering allocations.

## API review 6: generated validated-schema artifacts

This additive proposal is a generator-consumption ABI, not a generator-to-generator SPI. Static
contracts allow independently generated closed types to avoid reflection, dynamic code, runtime
schema processing, and validator compilation.

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+public interface IOpenApiValidatedJsonSchemaArtifact<TSelf>
+    where TSelf : IOpenApiValidatedJsonSchemaArtifact<TSelf>
+{
+    static abstract ReadOnlyMemory<byte> SourceSchema { get; }
+    static abstract ReadOnlyMemory<byte> NormalizedSchema { get; }
+    static abstract ReadOnlyMemory<byte> LocalReferences { get; }
+    static abstract OpenApiJsonSchemaDialect Dialect { get; }
+    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }
+    static abstract string SchemaIdentity { get; }
+    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);
+}
+public interface IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
+    where TArtifact : IOpenApiValidatedJsonSchemaArtifact<TArtifact>
+    where TSelf : IOpenApiValidatedJsonSchemaValidator<TArtifact, TSelf>
+{
+    static abstract string ConfigurationIdentity { get; }
+    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
+        ReadOnlyMemory<byte> utf8Json,
+        OpenApiSchemaEvidencePurpose purpose,
+        CancellationToken cancellationToken = default);
+}
+public interface IOpenApiValidatedJsonSchemaBinding<TSelf>
+    where TSelf : IOpenApiValidatedJsonSchemaBinding<TSelf>
+{
+    static abstract Type Type { get; }
+    static abstract string SchemaIdentity { get; }
+    static abstract string Identity { get; }
+    static abstract OpenApiJsonSchemaDialect Dialect { get; }
+    static abstract OpenApiJsonSchemaValidationCapabilities Capabilities { get; }
+    static abstract OpenApiSchema CreateOpenApiSchema(OpenApiSpecVersion openApiVersion);
+    static abstract ValueTask<OpenApiJsonSchemaValidationResult> ValidateAsync(
+        ReadOnlyMemory<byte> utf8Json,
+        OpenApiSchemaEvidencePurpose purpose,
+        CancellationToken cancellationToken = default);
+}

 namespace Microsoft.AspNetCore.Builder;

+public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
+    this IEndpointConventionBuilder builder,
+    OpenApiSchemaEvidencePurpose purpose)
+    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;
+public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
+    this IEndpointConventionBuilder builder,
+    OpenApiSchemaEvidencePurpose purpose,
+    OpenApiValidatedJsonSchemaOptions? options)
+    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;
+public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
+    this IEndpointConventionBuilder builder,
+    OpenApiSchemaEvidencePurpose purpose,
+    Func<ControllerActionDescriptor, bool> actionPredicate)
+    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;
+public static IEndpointConventionBuilder WithValidatedJsonSchema<TBinding>(
+    this IEndpointConventionBuilder builder,
+    OpenApiSchemaEvidencePurpose purpose,
+    Func<ControllerActionDescriptor, bool> actionPredicate,
+    OpenApiValidatedJsonSchemaOptions? options)
+    where TBinding : IOpenApiValidatedJsonSchemaBinding<TBinding>;
```

The artifact and validator are separate because schema identity is independent of validation
engine configuration. A closed binding supplies the endpoint-facing composite identity. Returning
`IEndpointConventionBuilder` is the cost of specifying only `TBinding`; generated convenience
extensions can preserve a concrete builder type. `CreateOpenApiSchema` intentionally couples the
experimental ABI to mutable OpenAPI.NET types and therefore must return an isolated model on every
call. A future reviewed replacement can change this shape while ASP0040 remains experimental.

## Existing unshipped APIs not introduced by this prototype

The current `PublicAPI.Unshipped.txt` also contains these entries from the merge base:

- `IAdditionalOpenApiDocumentNameResolver`;
- `IAdditionalOpenApiDocumentNameResolver.ResolveDocumentNames`;
- `OpenApiServiceCollectionExtensions.AddOpenApiCore`.

They are not part of this proposal and should be resolved through their existing ownership process.
