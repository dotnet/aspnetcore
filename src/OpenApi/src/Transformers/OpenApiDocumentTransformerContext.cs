// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Represents the context in which an OpenAPI document transformer is executed.
/// </summary>
public sealed class OpenApiDocumentTransformerContext
{
    /// <summary>
    /// Gets the name of the associated OpenAPI document.
    /// </summary>
    /// <remarks>
    /// This corresponds to the document name provided when calling
    /// <see cref="OpenApiServiceCollectionExtensions.AddOpenApi(IServiceCollection)">AddOpenApi</see> during service registration. The default document name is <c>"v1"</c>.
    /// </remarks>
    public required string DocumentName { get; init; }

    /// <summary>
    /// Gets the API description groups for the application.
    /// </summary>
    /// <remarks>
    /// Each <see cref="ApiDescriptionGroup"/> contains a collection of <see cref="ApiDescription"/>
    /// items that describe API endpoints. These descriptions provide metadata about each endpoint
    /// such as the HTTP method, relative path, supported request/response formats, and parameters.
    /// <para>
    /// This property contains all API descriptions from the application, not only the endpoints
    /// included in the current document. To determine which descriptions correspond to endpoints
    /// in this document, use <see cref="OpenApiOptions.ShouldInclude"/> to filter the descriptions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Filter to only the API descriptions included in this document
    /// var options = context.ApplicationServices
    ///     .GetRequiredService&lt;IOptionsMonitor&lt;OpenApiOptions&gt;&gt;()
    ///     .Get(context.DocumentName);
    /// var descriptions = context.DescriptionGroups
    ///     .SelectMany(g =&gt; g.Items)
    ///     .Where(options.ShouldInclude);
    /// foreach (var description in descriptions)
    /// {
    ///     Console.WriteLine($"{description.HttpMethod} {description.RelativePath}");
    /// }
    /// </code>
    /// </example>
    public required IReadOnlyList<ApiDescriptionGroup> DescriptionGroups { get; init; }

    /// <summary>
    /// Gets the application services associated with the current document.
    /// </summary>
    /// <remarks>
    /// This is the <see cref="IServiceProvider"/> used when generating the OpenAPI document and can
    /// be used to resolve application services within a document transformer. It is typically a
    /// scoped provider, such as <see cref="Http.HttpContext.RequestServices">HttpContext.RequestServices</see> or a scope created specifically
    /// for document generation, but the exact lifetime of resolved services depends on how the
    /// OpenAPI document generation was invoked.
    /// </remarks>
    /// <example>
    /// <code>
    /// var myService = context.ApplicationServices.GetRequiredService&lt;MyService&gt;();
    /// </code>
    /// </example>
    public required IServiceProvider ApplicationServices { get; init; }

    internal IOpenApiSchemaTransformer[] SchemaTransformers { get; init; } = [];

    // Internal because we expect users to interact with the `Document` provided in
    // the `IOpenApiDocumentTransformer` itself instead of the context object.
    internal OpenApiDocument? Document { get; init; }

    /// <summary>
    /// Gets or creates an <see cref="OpenApiSchema"/> for the specified type.
    /// </summary>
    /// <remarks>
    /// The returned schema is augmented with any <see cref="IOpenApiSchemaTransformer"/>s that are
    /// registered on the document. If <paramref name="parameterDescription"/> is not null, the schema
    /// will also be augmented with the <see cref="ApiParameterDescription"/> information, such as
    /// default values and validation metadata.
    /// </remarks>
    /// <param name="type">The type for which the schema is being created.</param>
    /// <param name="parameterDescription">An optional parameter description to augment the schema.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, with a value of type <see cref="OpenApiSchema"/>.</returns>
    public Task<OpenApiSchema> GetOrCreateSchemaAsync(Type type, ApiParameterDescription? parameterDescription = null, CancellationToken cancellationToken = default)
    {
        Debug.Assert(Document is not null, "Document should have been initialized by framework.");
        var schemaService = ApplicationServices.GetRequiredKeyedService<OpenApiSchemaService>(DocumentName);
        return schemaService.GetOrCreateUnresolvedSchemaAsync(
            document: Document,
            type: type,
            parameterDescription: parameterDescription,
            scopedServiceProvider: ApplicationServices,
            schemaTransformers: SchemaTransformers,
            cancellationToken: cancellationToken);
    }
}
