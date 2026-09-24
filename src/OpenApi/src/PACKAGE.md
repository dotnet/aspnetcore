## About

Microsoft.AspNetCore.OpenApi is a NuGet package that provides built-in support for generating OpenAPI documents from minimal or controller-based APIs in ASP.NET Core.

## Key Features

* Supports viewing generated OpenAPI documents at runtime via a parameterized endpoint (`/openapi/{documentName}.json`)
* Supports generating an OpenAPI document at build-time
* Supports customizing the generated document via document transformers

## How to Use

To start using Microsoft.AspNetCore.OpenApi in your ASP.NET Core application, follow these steps:

### Installation

```sh
dotnet add package Microsoft.AspNetCore.OpenApi
```

### Configuration

In your Program.cs file, register the services provided by this package in the DI container and map the provided OpenAPI document endpoint in the application.

```C#
var builder = WebApplication.CreateBuilder();

// Registers the required services
builder.Services.AddOpenApi();

var app = builder.Build();

// Adds the /openapi/{documentName}.json endpoint to the application
app.MapOpenApi();

app.Run();
```

### Version-specific transformers

The configured `OpenApiOptions.OpenApiVersion` is the generation target exposed to document,
operation, and schema transformers through their context's `OpenApiVersion` property. This allows
transformers to add JSON Schema keywords such as `if`, `then`, `else`, `dependentRequired`, and
`dependentSchemas` only when targeting OpenAPI 3.1 or later:

```C#
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(Payment) &&
            context.OpenApiVersion >= OpenApiSpecVersion.OpenApi3_1)
        {
            schema.DependentRequired = new Dictionary<string, HashSet<string>>
            {
                ["creditCard"] = ["billingAddress"],
            };
        }

        return Task.CompletedTask;
    });
});
```

Use `IOpenApiDocumentProvider.GetOpenApiDocumentForVersionAsync` to generate a document for a
target other than the configured default. Version-sensitive transformer output is generated for
that target. Do not serialize the returned model using a different OpenAPI version; regenerate it
for the desired target instead. OpenAPI 3.0 does not support conditional or dependent JSON Schema
keywords, so transformers targeting 3.0 should omit them rather than emit compatibility extensions.

To serialize `Tuple` and `ValueTuple` values as positional JSON arrays and emit matching schemas,
register the experimental tuple converter with the HTTP JSON options consumed by OpenAPI:

```C#
#pragma warning disable ASP0040
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonArrayTupleConverter());
});
#pragma warning restore ASP0040
```

Registration changes the runtime JSON contract, so matching tuple schemas are emitted in both
legacy and inferred schema-generation modes. OpenAPI 3.1 and 3.2 documents use ordered
`prefixItems`, exact `minItems` and `maxItems`, and `items: false`. OpenAPI 3.0 cannot represent
positional element schemas: it emits a conforming broad approximation with exact arity and a
single unconstrained `items` schema. The converter dynamically constructs closed converters and
is unsupported in NativeAOT and trimming-sensitive applications until generated closed converters
are available.

To opt in to inferred serializer-contract semantics for schema composition, configure the experimental schema generation mode:

```C#
#pragma warning disable ASP0040
builder.Services.AddOpenApi(options =>
{
    options.SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred;
});
#pragma warning restore ASP0040
```

This mode emits `oneOf` for System.Text.Json polymorphic contracts only when every configured
branch has a distinct, explicit discriminator. Other alternatives continue to use `anyOf`.
It also models serializer direction separately for endpoint inputs and outputs. Request bodies and
parameters use the effective deserialization contract, including constructor-parameter requiredness
when `JsonSerializerOptions.RespectRequiredConstructorParameters` is enabled. Responses use the
effective readable contract and do not treat C# `required`, `[JsonRequired]`, or
`JsonPropertyInfo.IsRequired` as proof that a property is always emitted. Conditional ignore
policies therefore keep a readable property described without making it response-required.
Get-only and set-only members can consequently produce distinct input and output components.
Nullable uses wrap component references rather than changing the shared component.

When a serializer contract has different effective shapes in the two directions, inferred mode
assigns deterministic `.Input` and `.Output` component IDs. Types used in only one direction, and
bidirectional graphs whose effective shapes are identical, retain their existing component names.
Schema requests made explicitly by transformers remain direction-neutral and do not silently
inherit the purpose of the endpoint currently being generated. Legacy mode continues to use its
existing shared, version-neutral schema contract.

It also emits ordered `oneOf` branches for C# unions only when immutable serializer-contract facts
prove every pair of JSON instance domains disjoint. The proof distinguishes null, boolean, string,
integer, non-integer number, object, and array domains; unrestricted numbers overlap integers.
Numeric domains also follow effective System.Text.Json number handling. Reading or writing numbers
as strings adds the string domain, and named floating-point literals add it for IEEE floating-point
types. Consequently, the ASP.NET Web default keeps string-and-number unions as `anyOf`; configuring
strict number handling can make string-and-number branches provably disjoint.

For minimal APIs, configure the same HTTP JSON options consumed by OpenAPI generation when strict
numeric contracts are required:

```C#
using System.Text.Json.Serialization;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
```

Object-object and array-array branches overlap, and multiple nullable branches overlap on null.
For non-flags string enums configured to reject integer values, inferred mode can use the finite
literal set emitted by System.Text.Json. Two such enum branches are exclusive only when their
canonical JSON literal sets are disjoint. A finite string enum still overlaps unrestricted string,
and nullable finite enums overlap on null. Numeric literals use JSON Schema mathematical equality,
so equivalent forms such as `1` and `1.0` are the same value.

Numeric enums, flags enums, string enum converters that allow integer values, custom converters,
arbitrary JSON values (`object`, `JsonElement`, and JSON nodes), polymorphic contracts, and
unsupported scalar representations remain `anyOf` because their accepted values are not proven
closed. System.Text.Json intentionally omits the integer alternative from a string-enum schema
even when its converter accepts integers; inferred mode verifies the serializer contract and does
not mistake that schema for a closed set. Union `oneOf` schemas do not add discriminators.
It also emits `allOf` for inheritance only when the serializer contracts can be separated
losslessly into a reusable base component and local derived properties. Contracts with custom
converters, property collisions or hiding, extension data, additional-properties constraints,
incompatible base properties, or polymorphic bases remain flattened.

Mixed object contracts with System.Text.Json extension data retain their named properties and
required entries while using the extension-data value contract for `additionalProperties`.
`Dictionary<string, JsonElement>`, `Dictionary<string, object>`, and their supported
`IDictionary<string, ...>` forms produce an unconstrained fallback schema, matching the values
accepted by System.Text.Json. Pure dictionaries keep their dictionary-only schema, and contracts
that disallow unmapped members emit `additionalProperties: false`. Open object contracts remain
ineligible for inferred `allOf` decomposition.

Only extension-data forms accepted by System.Text.Json are inferred. In particular, arbitrary
strongly typed dictionary values are not treated as extension data, and invalid forms fail through
the serializer's normal contract validation. `JsonObject` extension data is not inferred because
System.Text.Json metadata does not expose a reliable fallback value contract for that form. Schema
transformers visit the root, named properties in serializer order, then the extension-data fallback
with its `JsonPropertyInfo`; they do not receive a duplicate callback for the extension-data
dictionary property.

Schema transformers continue to run once for the composed derived schema and once for each
serialized property, in serializer order. The synthetic base and local `allOf` branches do not
introduce additional transformer callbacks.

Collection and dictionary schemas follow the effective System.Text.Json serialization contract,
not the CLR interfaces implemented by a type. Enumerable contracts emit arrays with the item
schema exposed by `JsonTypeInfo`; dictionary contracts emit objects with the value schema in
`additionalProperties`. This includes supported immutable, frozen, and read-only contracts.
Properties added by collection or dictionary subclasses are not serialized and are not included
in the schema. Custom converters remain unconstrained unless they provide package-recognized
schema provenance.

The generated schema does not infer `uniqueItems` for set types because System.Text.Json accepts
duplicate JSON array entries and coalesces them during materialization rather than validating
uniqueness. It also does not infer `propertyNames` or a finite dictionary-key domain. The public
System.Text.Json contract metadata does not expose the effective property-name converter, and
dictionary key policies and custom converters can change the serialized names. These constraints
are omitted in OpenAPI 3.0, 3.1, and 3.2 rather than emitted as unsupported compatibility
extensions. Applications with an authoritative key or uniqueness contract can add the applicable
keywords explicitly in a version-aware schema transformer.

Scalar schemas combine three related but distinct layers:

* The effective System.Text.Json contract determines which JSON values the configured converter
  reads and writes. Number handling, custom converters, and source-generated metadata are part of
  this layer.
* JSON Schema formats describe standardized lexical spaces. A format is not proof of a .NET
  converter's complete accepted-value domain, and OpenAPI 3.1 and later treat formats as
  annotations unless validation is explicitly enabled.
* OpenAPI formats are also widely consumed as client-generation hints. Formats such as `int32`,
  `int64`, `float`, `double`, and `byte` can select a useful target-language type even when they do
  not completely describe serializer validation.

Legacy mode preserves its established client hints and ignores inferred scalar-format options.
Inferred mode defaults to `OpenApiScalarFormatPolicy.Conventional`, which emits well-known formats
that are useful to validators and generated clients even when a runtime converter or binder accepts
a broader lexical language. `CompatibleOnly` retains only formats compatible with the complete
package-proven contract, and `None` suppresses optional formats:

```C#
#pragma warning disable ASP0040
builder.Services.AddOpenApi(options =>
{
    options.SchemaGenerationMode = OpenApiSchemaGenerationMode.Inferred;
    options.ScalarFormatPolicy = OpenApiScalarFormatPolicy.CompatibleOnly;
    options.CreateScalarFormat = context =>
        context.EffectiveType == typeof(MyIdentifier) ? "my-identifier" : context.DefaultFormat;
});
#pragma warning restore ASP0040
```

`CreateScalarFormat` runs for every inferred scalar context, including custom converters and
parsers for which the policy has no candidate. Returning `context.DefaultFormat` accepts the
policy result, returning another string replaces it verbatim, and returning `null` suppresses it.
The context identifies the declared and effective CLR types, JSON or transport location,
input/output purpose, target OpenAPI version, and package-recognized converter or parser
provenance. Scalar formats are finalized before schema transformers run, so transformers retain
final authority.

| CLR contract | Conventional | CompatibleOnly | None |
| --- | --- | --- | --- |
| `Guid` | `uuid` | `uuid` for built-in JSON; none for transport | None |
| `Uri` | `uri-reference` | None | None |
| `DateOnly` | `date` | `date` for built-in JSON; none for transport | None |
| `DateTime`, `DateTimeOffset` | `date-time` | None | None |
| `TimeOnly` | `time` | None | None |
| `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong` | Width hint | Width hint | None |
| `float`, `double`, `char` | `float`, `double`, or `char` | None | None |
| `TimeSpan`, `decimal`, `Half`, `sbyte`, `Int128`, `UInt128`, `nint`, `nuint`, `BigInteger`, `Version`, `Rune`, `IPAddress`, `IPEndPoint` | None | None | None |
| Custom or opaque converter/parser | None | None | None |

Proven base64 representation is not an optional scalar-format choice. For `byte[]`,
`Memory<byte>`, and `ReadOnlyMemory<byte>`, Inferred mode always emits the OpenAPI 3.0
`format: byte` fallback or OpenAPI 3.1/3.2 `contentEncoding: base64`, regardless of policy or
callback result.

Numeric schemas follow effective `JsonNumberHandling` for their JSON type alternatives and lexical
patterns. Reading or writing numbers as strings adds a string alternative, and named IEEE
floating-point literals add `"NaN"`, `"Infinity"`, and `"-Infinity"`. Inferred mode adds exact
`minimum` and `maximum` constraints for built-in signed and unsigned integral converters, including
128-bit integers. Numeric keywords apply only to numeric instances, so
quoted-number alternatives remain valid. Floating-point and decimal schemas remain unbounded;
`multipleOf` is not inferred. Width formats remain client hints for the established integral and
floating-point cases, while `sbyte`, `Int128`, `UInt128`, and `Half` have no width format.

Non-body parameter binding is a separate contract from JSON serialization. Inferred mode builds
immutable transport facts from the effective route, query, header, or form binding metadata and
then makes transport-specific schema decisions. Package-recognized numeric binders use their
logical post-binding JSON Schema type. Fixed-width integral binders include their exact CLR
minimum and maximum, while floating-point and decimal binders remain unbounded. Repeated values
use arrays whose item schema is inferred from the element binder.

Text-parsed framework types use broad string schemas. The conventional policy can annotate those
schemas with the formats listed above, including `uri-reference` for relative or absolute URI
values, without changing the represented JSON type. Enum binding accepts member names, numeric
values, and flags combinations; its conservative schema therefore permits both a broad string
branch and the bounded underlying integral branch. Custom `TryParse` and `IParsable` contracts use
broad strings because endpoint metadata proves that text parsing occurs but cannot prove the
parser's language; they receive no policy candidate, but applications can opt in through
`CreateScalarFormat`. `BindAsync` does not imply a text contract and remains uninferred.

Transport schemas are kept separate from System.Text.Json body components and are finalized before
schema transformers run. Form fields use the same transport decisions even though OpenAPI
represents them as properties of a form request-body object. Nullable and defaulted transport
parameters express absence through parameter/property requiredness; their schemas do not add a
JSON `null` value. These rules apply consistently in OpenAPI 3.0, 3.1, and 3.2.

Scalar compatibility coverage records serializer, exporter, emitted-schema, format-policy, and
parameter-binding behavior. Inferred mode uses converter-proven body facts, version-appropriate
base64 encoding, and the separate binder-aware transport decisions described above. Applications
can select or suppress scalar annotations through the callback and can provide stricter
domain-specific constraints with a version-aware schema transformer.

The inferred mode also resolves component names from the complete set of serializer contracts
used by the document before schemas are emitted. A default name that is unique is unchanged. Name
collisions are resolved deterministically by adding declaring-type or namespace segments, with a
stable canonical fallback, so endpoint registration order does not affect component keys or
references. Types first requested by a transformer after endpoint discovery use the stable
canonical fallback immediately for their complete inferred graph, so their regular and
polymorphic component names do not depend on transformer execution order.
JSON Patch document variants retain their intentional shared component.

Canonical fallback hashes include the full assembly identity. Types loaded into separate assembly
load contexts with the same assembly-qualified identity remain indistinguishable for naming
purposes; if they would occupy the same component ID, document generation fails explicitly.

Custom `CreateSchemaReferenceId` values are authoritative in inferred mode. A `null` value still
inlines the schema. Empty or invalid values, or the same non-null value returned for distinct
non-aliased serializer contract types, cause document generation to fail rather than silently
selecting or overwriting a component.

When the Request Delegate Generator (RDG) handles a minimal API endpoint, it automatically
registers reflection-free closed tuple converters before the HTTP JSON serializer options become
read-only. Automatic discovery covers tuple contracts used directly as JSON request bodies or
serializable responses, arrays, and public readable properties (including inherited properties)
on source-declared DTO classes and structs. Registration is deterministic and idempotent, and an
existing user converter that handles the same tuple contract takes precedence. This behavior does
not require `AddOpenApi`; when OpenAPI is present, schema inference recognizes the same converter
provenance and emits the matching tuple schema.

Automatic discovery is intentionally bounded and does not reproduce runtime System.Text.Json
contract discovery. Dynamic or non-RDG endpoints, metadata-only DTO graphs, fields, arbitrary
collection or dictionary graphs, open generic contracts, polymorphic contracts, and types or
properties with custom converters require explicit registration. Applications can use the
reflection-free closed converters as that fallback and for any tuple contracts known at compile
time:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        JsonArrayTupleConverters.CreateValueTuple<int, string>());
});
```

Register each closed `Tuple` or `ValueTuple` contract that the application uses. Contracts with
more than seven elements use the CLR `Rest` encoding and compose another closed converter:

```csharp
var rest = JsonArrayTupleConverters.CreateValueTuple<DateTime, decimal>();
options.SerializerOptions.Converters.Add(
    JsonArrayTupleConverters.CreateValueTuple<int, int, int, int, int, int, int, ValueTuple<DateTime, decimal>>(rest));
```

The closed converters use configured `JsonTypeInfo` metadata for every element and require no
runtime reflection or dynamic code. They produce the same positional runtime JSON and OpenAPI
schemas as `JsonArrayTupleConverter`: exact ordered `prefixItems` schemas for OpenAPI 3.1 and 3.2,
and the exact-arity, unconstrained-element approximation for OpenAPI 3.0. The
`JsonArrayTupleConverter` convenience factory remains available for applications that can use
dynamic code, but it is unsupported in trimming-sensitive or NativeAOT applications.

For more information on configuring and using Microsoft.AspNetCore.OpenApi, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/openapi).

## Build-time Document Generation

Microsoft.AspNetCore.OpenApi supports generating OpenAPI documents at build time via the `Microsoft.Extensions.ApiDescription.Server` package. When you run `dotnet build`, the document generation tool runs automatically.

### Viewing Build-time Generation Output

By default, the .NET 8+ Terminal Logger suppresses the document generation tool output. To see the full generation logs, including messages like `Generating document named 'v1'`, use one of the following approaches:

* **Increase Terminal Logger verbosity**: Pass `-tlp:v=d` (detailed) to your build command:

  ```sh
  dotnet build -tlp:v=d
  ```

* **Disable the Terminal Logger**: Pass `--tl:off` to fall back to the classic logger, which shows all tool output:

  ```sh
  dotnet build --tl:off
  ```

This is particularly useful when debugging OpenAPI document generation issues.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `OpenApiOptions`: Options for configuring OpenAPI document generation.
* `IDocumentTransformer`: Transformer that modifies the OpenAPI document generated by the library.

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.AspNetCore.OpenApi is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).