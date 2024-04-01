// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDocumentService(
    [ServiceKey] string documentName,
    IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
    IHostEnvironment hostEnvironment,
    IOptionsSnapshot<OpenApiOptions> optionsSnapshot)
{
    private readonly OpenApiOptions _options = optionsSnapshot.Get(documentName);

    public Task<OpenApiDocument> GetOpenApiDocumentAsync()
    {
        var document = new OpenApiDocument
        {
            Info = GetOpenApiInfo(),
            Paths = GetOpenApiPaths()
        };
        return Task.FromResult(document);
    }

    internal OpenApiInfo GetOpenApiInfo()
    {
        return new OpenApiInfo
        {
            Title = $"{hostEnvironment.ApplicationName} | {documentName}",
            Version = OpenApiConstants.DefaultOpenApiVersion
        };
    }

    /// <summary>
    /// Gets the OpenApiPaths for the document based on the ApiDescriptions.
    /// </summary>
    /// <remarks>
    /// At this point in the construction of the OpenAPI document, we run
    /// each API description through the `ShouldInclude` delegate defined in
    /// the object to support filtering each
    /// description instance into its appropriate document.
    /// </remarks>
    internal OpenApiPaths GetOpenApiPaths()
    {
        var descriptionsByPath = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(_options.ShouldInclude)
            .GroupBy(apiDescription => apiDescription.MapRelativePathToItemPath());
        var paths = new OpenApiPaths();
        foreach (var descriptions in descriptionsByPath)
        {
            Debug.Assert(descriptions.Key != null, "Relative path mapped to OpenApiPath key cannot be null.");
            paths.Add(descriptions.Key, new OpenApiPathItem { Operations = GetOperations(descriptions) });
        }
        return paths;
    }

    internal static Dictionary<OperationType, OpenApiOperation> GetOperations(IGrouping<string?, ApiDescription> descriptions)
    {
        var operations = new Dictionary<OperationType, OpenApiOperation>();
        foreach (var description in descriptions)
        {
            operations[description.ToOperationType()] = new OpenApiOperation();
        }
        return operations;
    }
}
