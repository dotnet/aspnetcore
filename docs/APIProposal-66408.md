# OpenAPI extensibility for API versioning

## Background and Motivation

`Microsoft.AspNetCore.OpenApi` can generate OpenAPI documents for named documents registered up front with `AddOpenApi(documentName)`. ASP.NET API Versioning needs to generate one OpenAPI document per API version, but its document names come from `IApiVersionDescriptionProvider` after the application services have been composed. The current OpenAPI implementation keeps the types that generate documents, generate schemas, track document names, and support build-time `dotnet getdocument` behind internal APIs. As a result, `Asp.Versioning.OpenApi` has to use reflection and expression-compiled constructors to:

- set `OpenApiOptions.DocumentName` for each API version;
- construct `OpenApiSchemaService` and `OpenApiDocumentService` for API-version document names;
- recreate the internal keyed service registrations expected by `MapOpenApi` and build-time generation; and
- register the internal `Microsoft.Extensions.ApiDescriptions.IDocumentProvider` contract used by `dotnet getdocument`.

Those reflection-based workarounds are slower, complicate native AOT/trimming support, and couple `Asp.Versioning.OpenApi` to internal implementation details in `Microsoft.AspNetCore.OpenApi`.

Rather than making the current internal implementation types public, this proposal introduces a small public document-generation abstraction that lets external libraries supply document names and generate OpenAPI documents through supported APIs. The OpenAPI package can continue to own the concrete implementation details of schema services, document services, keyed service registration, and build-time generation.

## Proposed API

```diff
namespace Microsoft.AspNetCore.OpenApi;

public sealed class OpenApiOptions
{
-    public string DocumentName { get; }
+    public string DocumentName { get; set; }
}
+
+public interface IOpenApiDocumentNameProvider
+{
+    IEnumerable<string> GetDocumentNames();
+}
+
+public sealed class OpenApiDocumentGenerationContext
+{
+    public required string DocumentName { get; init; }
+    public required IServiceProvider ApplicationServices { get; init; }
+    public HttpRequest? HttpRequest { get; init; }
+    public CancellationToken CancellationToken { get; init; }
+}
+
+public sealed class OpenApiDocumentGenerator
+{
+    public OpenApiDocumentGenerator(IServiceProvider serviceProvider);
+    public Task<OpenApiDocument> GenerateAsync(OpenApiDocumentGenerationContext context);
+}
+
+namespace Microsoft.Extensions.DependencyInjection;
+
+public static class OpenApiServiceCollectionExtensions
+{
+    public static IServiceCollection AddOpenApiDocumentGeneration(this IServiceCollection services);
+}
```

The existing `AddOpenApi(...)` overloads would call `AddOpenApiDocumentGeneration(...)` internally and register an `IOpenApiDocumentNameProvider` for each explicitly configured document name. The internal build-time `IDocumentProvider` implementation would enumerate `IOpenApiDocumentNameProvider` instances and call `OpenApiDocumentGenerator`. `MapOpenApi` would use `OpenApiDocumentGenerator` so that documents from external `IOpenApiDocumentNameProvider` implementations can be served without requiring callers to recreate internal keyed service registrations.

No `EditorBrowsable(EditorBrowsableState.Never)` annotations are proposed. The new APIs are intended public extension points. `OpenApiOptions.DocumentName` uses normal public getter/setter accessibility instead of a public getter with an internal or hidden setter.

The following types remain internal implementation details:

```diff
namespace Microsoft.AspNetCore.OpenApi;

- internal sealed class OpenApiSchemaService;
- internal sealed class OpenApiDocumentService;
- internal sealed class NamedService<TService>;

namespace Microsoft.Extensions.ApiDescriptions;

- internal interface IDocumentProvider;
- internal sealed class OpenApiDocumentProvider;
```

## Usage Examples

`Asp.Versioning.OpenApi` can register OpenAPI document generation once and provide API-version document names dynamically:

```csharp
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;

builder.Services.AddOpenApiDocumentGeneration();
builder.Services.AddSingleton<IOpenApiDocumentNameProvider, ApiVersionOpenApiDocumentNameProvider>();
builder.Services.AddTransient<IPostConfigureOptions<OpenApiOptions>, ConfigureVersionedOpenApiOptions>();

internal sealed class ApiVersionOpenApiDocumentNameProvider(
    IApiVersionDescriptionProvider provider) : IOpenApiDocumentNameProvider
{
    public IEnumerable<string> GetDocumentNames()
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            yield return description.GroupName.ToLowerInvariant();
        }
    }
}

internal sealed class ConfigureVersionedOpenApiOptions(
    IApiVersionDescriptionProvider provider,
    VersionedOpenApiOptionsFactory factory) : IPostConfigureOptions<OpenApiOptions>
{
    public void PostConfigure(string? name, OpenApiOptions options)
    {
        var description = provider.ApiVersionDescriptions
            .SingleOrDefault(description => string.Equals(description.GroupName, name, StringComparison.OrdinalIgnoreCase));

        if (description is null)
        {
            return;
        }

        var versionedOptions = factory.Create(description);
        var apiExplorer = new ApiExplorerTransformer(versionedOptions);

        options.DocumentName = description.GroupName;
        options.ShouldInclude = versionedOptions.ShouldInclude;
        options.AddDocumentTransformer(apiExplorer);
        options.AddSchemaTransformer(apiExplorer);
        options.AddOperationTransformer(apiExplorer);
    }
}
```

If a library needs to generate a document directly, it can use the generator instead of constructing internal services:

```csharp
public sealed class VersionedOpenApiEndpoint(OpenApiDocumentGenerator generator)
{
    public async Task WriteAsync(HttpContext context, string documentName)
    {
        var document = await generator.GenerateAsync(new OpenApiDocumentGenerationContext
        {
            DocumentName = documentName,
            ApplicationServices = context.RequestServices,
            HttpRequest = context.Request,
            CancellationToken = context.RequestAborted,
        });

        await document.SerializeAsync(new OpenApiJsonWriter(context.Response.Body), OpenApiSpecVersion.OpenApi3_1);
    }
}
```

For the common case, applications can keep using the existing APIs:

```csharp
builder.Services.AddOpenApi("v1");
app.MapOpenApi();
```

## Alternative Designs

### Original issue proposal

The original proposal made these implementation types public: `OpenApiSchemaService`, `OpenApiDocumentService`, `NamedService<TService>`, `Microsoft.Extensions.ApiDescriptions.IDocumentProvider`, and `Microsoft.Extensions.ApiDescriptions.OpenApiDocumentProvider`. It also made `OpenApiOptions.DocumentName` settable, but hid parts of the API with `EditorBrowsable(EditorBrowsableState.Never)`.

This proposal intentionally diverges from that approach. `Asp.Versioning.OpenApi` only needs to provide versioned document names, configure options for those names, and ask ASP.NET Core OpenAPI to generate or serve a document. It does not need a supported contract for the concrete schema cache, document service constructor, named-service workaround for keyed DI enumeration, or the build-time `getdocument` implementation type. Keeping those details internal makes future OpenAPI implementation changes easier while still eliminating reflection in API Versioning.

### Public `IDocumentProvider` only

Making `Microsoft.Extensions.ApiDescriptions.IDocumentProvider` public would let third-party libraries register with `dotnet getdocument`, but it would not address runtime `MapOpenApi`, schema/document service construction, or document-name enumeration. API Versioning would still need reflection or duplicated internal service registration for runtime documents.

### Public `OpenApiDocumentService` and `OpenApiSchemaService`

Making the existing services public is the smallest code change in ASP.NET Core, but it exposes constructor dependencies and caching behavior that are currently implementation details. It also pushes keyed-service composition and name tracking onto every third-party library that wants dynamic OpenAPI documents.

### New `AddOpenApi(Func<IServiceProvider, IEnumerable<string>> documentNames, ...)` overload

An overload that accepts a document-name factory could work for API Versioning, but it would couple dynamic document names to service registration and would not give advanced libraries an explicit document-generation service. The proposed `IOpenApiDocumentNameProvider` composes naturally with DI and can support multiple libraries contributing document names.

## Risks

- `OpenApiOptions.DocumentName` becoming settable is a new mutability point. Invalid or inconsistent document names could produce surprising filtering behavior, so implementations should validate for `null` and normalize names in the same way as existing `AddOpenApi` code.
- `IOpenApiDocumentNameProvider.GetDocumentNames()` can be backed by dynamic data. `MapOpenApi` and build-time generation should treat returned names as a snapshot for each operation and preserve existing case-insensitive document-name behavior.
- `OpenApiDocumentGenerator` becomes a supported public abstraction. Its contract should avoid exposing current constructor dependencies so that schema caching and transformer initialization can evolve internally.
- This is a slightly larger implementation change than simply changing `internal` to `public`, so it may be less suitable for a servicing release. The benefit is a smaller, purpose-built public API surface that avoids committing to internal implementation types.
- Existing applications using `AddOpenApi(...)` should continue to behave the same because those overloads can register static document-name providers internally.
