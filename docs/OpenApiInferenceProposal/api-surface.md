# Experimental OpenAPI inference: proposed API surface

This appendix inventories the complete public API added by the prototype relative to merge base `89ab93803f3fcbb928f8ed1523945f89284f7e79`.

The APIs should not be reviewed as one indivisible block. They fall into five independently reviewable proposals. All prototype APIs use the existing experimental diagnostic `ASP0040`.

## API review 5: endpoint-enforced validated JSON Schema

This proposal is independent of inferred generation and the evidence-provider seam. It atomically binds exact immutable Draft 2020-12 bytes, a compiled validator, directional endpoint metadata, and bounded request/response enforcement.

```diff
 namespace Microsoft.AspNetCore.OpenApi;

+public enum OpenApiJsonSchemaDialect { Draft202012 }
+[Flags] public enum OpenApiJsonSchemaValidationCapabilities { None, FormatAssertions }
+public sealed class OpenApiValidatedJsonSchemaEvidence : OpenApiSchemaEvidence
+{
+    public JsonElement Schema { get; }
+    public OpenApiJsonSchemaDialect Dialect { get; }
+    public string Identity { get; }
+    public OpenApiJsonSchemaValidationCapabilities ValidationCapabilities { get; }
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
+}
```

Every declaration above carries `Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")`.

The result is a value type so `default`/`Valid` is the allocation-free success representation. Error collections are created only on failure. Endpoint conventions compile validators and precompute directional selectors and validation contexts once while building the endpoint. At runtime, bounded `ArrayPool<byte>` buffers and a pooled `IHttpResponseBodyFeature` capture raw request/response bytes; synchronous successful validation and copying avoid async state-machine, metadata, result, and buffering allocations.

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
+public sealed class OpenApiScalarSchemaEvidence : OpenApiSchemaEvidence
+{
+    public OpenApiScalarSchemaEvidence(
+        OpenApiScalarSchemaValueKind valueKind,
+        string? format = null,
+        string? pattern = null,
+        BigInteger? minimum = null,
+        BigInteger? maximum = null);
+
+    public OpenApiScalarSchemaValueKind ValueKind { get; }
+    public string? Format { get; }
+    public string? Pattern { get; }
+    public BigInteger? Minimum { get; }
+    public BigInteger? Maximum { get; }
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

`OpenApiSchemaEvidenceContext` has an internal constructor and is a framework-supplied provider input. This optional seam strengthens otherwise opaque contracts only when a third-party runtime mechanism enforces the returned evidence. It is not required for ordinary inferred generation. The closed algebra supports strict scalars and fixed positional arrays only; full objects, conditionals, composition, and arbitrary schema import remain unsupported and transformer-authored. Provider format values are policy-controlled annotation candidates; patterns and bounds carry enforceable lexical and range semantics.

## Existing unshipped APIs not introduced by this prototype

The current `PublicAPI.Unshipped.txt` also contains these entries from the merge base:

- `IAdditionalOpenApiDocumentNameResolver`;
- `IAdditionalOpenApiDocumentNameResolver.ResolveDocumentNames`;
- `OpenApiServiceCollectionExtensions.AddOpenApiCore`.

They are not part of this proposal and should be resolved through their existing ownership process.

## Review sequencing

The architecture design proposal can discuss all four capabilities together because they share serializer/schema-generation boundaries. Public API approval should remain separable:

1. inferred schema generation and scalar-format policy;
2. version-targeted generation and transformer visibility;
3. positional tuple JSON conversion and generated registration behavior;
4. runtime-enforced schema evidence providers.

Approval or rejection of one API package should not require the same outcome for the others.
