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
It also emits ordered `oneOf` branches for C# unions only when immutable serializer-contract facts
prove every pair of JSON instance domains disjoint. The proof distinguishes null, boolean, string,
integer, non-integer number, object, and array domains; unrestricted numbers overlap integers.
Object-object and array-array branches overlap, and multiple nullable branches overlap on null.
Enums, arbitrary JSON values (`object`, `JsonElement`, and JSON nodes), polymorphic contracts,
custom converters, and unsupported scalar representations remain `anyOf` because their domains
are not proven exact. Union `oneOf` schemas do not add discriminators.
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