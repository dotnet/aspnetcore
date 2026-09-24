# Experimental OpenAPI inference: proposed API surface

This appendix inventories the complete public API added by the prototype relative to merge base `89ab93803f3fcbb928f8ed1523945f89284f7e79`.

The APIs should not be reviewed as one indivisible block. They fall into three independently reviewable proposals. All prototype APIs use the existing experimental diagnostic `ASP0040`.

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

`OpenApiScalarFormatContext` has an internal constructor and is callback input only. Legacy is the default schema-generation mode. Conventional is the default inferred scalar-format policy.

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

## Existing unshipped APIs not introduced by this prototype

The current `PublicAPI.Unshipped.txt` also contains these entries from the merge base:

- `IAdditionalOpenApiDocumentNameResolver`;
- `IAdditionalOpenApiDocumentNameResolver.ResolveDocumentNames`;
- `OpenApiServiceCollectionExtensions.AddOpenApiCore`.

They are not part of this proposal and should be resolved through their existing ownership process.

## Review sequencing

The architecture design proposal can discuss all three capabilities together because they share serializer/schema-generation boundaries. Public API approval should remain separable:

1. inferred schema generation and scalar-format policy;
2. version-targeted generation and transformer visibility;
3. positional tuple JSON conversion and generated registration behavior.

Approval or rejection of one API package should not require the same outcome for the others.
