// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using System.Linq;

namespace Microsoft.Extensions.ApiDescriptions;

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
        var optionsSnapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenApiOptions>>();
        var namedOption = optionsSnapshot.Get(documentName);
        var resolvedOpenApiVersion = namedOption.OpenApiVersion;
        await GenerateAsync(documentName, writer, resolvedOpenApiVersion);
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
        // Microsoft.OpenAPI does not provide async APIs for writing the JSON
        // document to a file. See https://github.com/microsoft/OpenAPI.NET/issues/421 for
        // more info.
        var targetDocumentService = serviceProvider.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
        var document = await targetDocumentService.GetOpenApiDocumentAsync();
        var jsonWriter = new ScrubbingOpenApiJsonWriter(writer);
        document.Serialize(jsonWriter, openApiSpecVersion);
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
