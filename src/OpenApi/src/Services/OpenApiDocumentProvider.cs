// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Writers;
using System.Linq;

namespace Microsoft.Extensions.ApiDescriptions;

/// <summary>
/// Provides an implementation of <see cref="IDocumentProvider"/> to use for build-time generation of OpenAPI documents.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
internal sealed class OpenApiDocumentProvider(IServiceProvider serviceProvider) : IDocumentProvider
{
    /// <summary>
    /// Serializes the OpenAPI document associated with a given document name to
    /// the provided writer.
    /// </summary>
    /// <param name="documentName">The name of the document to resolve.</param>
    /// <param name="writer">A text writer associated with the document to write to.</param>
    public async Task GenerateAsync(string documentName, TextWriter writer)
    {
        // See OpenApiServiceCollectionExtensions.cs to learn why we lowercase the document name
        var lowercasedDocumentName = documentName.ToLowerInvariant();

        var options = serviceProvider.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        var namedOption = options.Get(lowercasedDocumentName);
        var resolvedOpenApiVersion = namedOption.OpenApiVersion;
        await GenerateAsync(lowercasedDocumentName, writer, resolvedOpenApiVersion);
    }

    /// <summary>
    /// Serializes the OpenAPI document associated with a given document name to
    /// the provided writer under the provided OpenAPI spec version.
    /// </summary>
    /// <param name="documentName">The name of the document to resolve.</param>
    /// <param name="writer">A text writer associated with the document to write to.</param>
    /// <param name="openApiSpecVersion">The OpenAPI specification version to use when serializing the document.</param>
    public async Task GenerateAsync(string documentName, TextWriter writer, OpenApiSpecVersion openApiSpecVersion)
    {
        // We need to retrieve the document name in a case-insensitive manner to support case-insensitive document name resolution.
        // The document service is registered with a key equal to the document name, but in lowercase.
        // The GetRequiredKeyedService() method is case-sensitive, which doesn't work well for OpenAPI document names here,
        // as the document name is also used as the route to retrieve the document, so we need to ensure this is lowercased to achieve consistency with ASP.NET Core routing.
        // See OpenApiServiceCollectionExtensions.cs for more info.
        var lowercasedDocumentName = documentName.ToLowerInvariant();

        // Microsoft.OpenAPI does not provide async APIs for writing the JSON
        // document to a file. See https://github.com/microsoft/OpenAPI.NET/issues/421 for
        // more info.
        var targetDocumentService = serviceProvider.GetRequiredKeyedService<OpenApiDocumentService>(lowercasedDocumentName);
        using var scopedService = serviceProvider.CreateScope();
        var document = await targetDocumentService.GetOpenApiDocumentAsync(scopedService.ServiceProvider);
        var jsonWriter = new OpenApiJsonWriter(writer);
        await document.SerializeAsync(jsonWriter, openApiSpecVersion);
    }

    /// <summary>
    /// Provides all document names that are currently managed in the application.
    /// </summary>
    public IEnumerable<string> GetDocumentNames()
    {
        // Keyed services lack an API to resolve all registered keys.
        // We use the service provider to resolve an internal type.
        // This type tracks registered document names.
        // See https://github.com/dotnet/runtime/issues/100105 for more info.
        var documentServices = serviceProvider.GetServices<NamedService<OpenApiDocumentService>>();
        return documentServices.Select(docService => docService.Name);
    }
}
