// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDocumentService(
    [ServiceKey] string documentName,
    IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
    IHostEnvironment hostEnvironment,
    IOptionsMonitor<OpenApiOptions> optionsMonitor,
    IServiceProvider serviceProvider)
{
    private readonly OpenApiOptions _options = optionsMonitor.Get(documentName);

    /// <summary>
    /// Cache of <see cref="OpenApiOperationTransformerContext"/> instances keyed by the
    /// `ApiDescription.ActionDescriptor.Id` of the associated operation. ActionDescriptor IDs
    /// are unique within the lifetime of an application and serve as helpful associators between
    /// operations, API descriptions, and their respective transformer contexts.
    /// </summary>
    private readonly ConcurrentDictionary<string, OpenApiOperationTransformerContext> _operationTransformerContextCache = new();

    internal bool TryGetCachedOperationTransformerContext(string descriptionId, [NotNullWhen(true)] out OpenApiOperationTransformerContext? context)
    {
        context = null;
        if (_operationTransformerContextCache.TryGetValue(descriptionId, out var cachedContext))
        {
            context = cachedContext;
            return true;
        }
        return false;
    }

    public async Task<OpenApiDocument> GetOpenApiDocumentAsync(CancellationToken cancellationToken = default)
    {
        // For good hygiene, operation-level tags must also appear in the document-level
        // tags collection. This set captures all tags that have been seen so far.
        HashSet<OpenApiTag> capturedTags = new(OpenApiTagComparer.Instance);
        var document = new OpenApiDocument
        {
            Info = GetOpenApiInfo(),
            Paths = GetOpenApiPaths(capturedTags),
            Tags = [.. capturedTags]
        };
        await ApplyTransformersAsync(document, cancellationToken);
        return document;
    }

    private async Task ApplyTransformersAsync(OpenApiDocument document, CancellationToken cancellationToken)
    {
        var documentTransformerContext = new OpenApiDocumentTransformerContext
        {
            DocumentName = documentName,
            ApplicationServices = serviceProvider,
            DescriptionGroups = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items,
        };
        // Use index-based for loop to avoid allocating an enumerator with a foreach.
        for (var i = 0; i < _options.DocumentTransformers.Count; i++)
        {
            var transformer = _options.DocumentTransformers[i];
            try
            {
                await transformer.TransformAsync(document, documentTransformerContext, cancellationToken);
            }
            finally
            {
                if (transformer is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                if (transformer is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                if (transformer is TypeBasedOpenApiDocumentTransformer typedTransformer)
                {
                    await typedTransformer.DisposeAsync();
                }
            }
        }
    }

    // Note: Internal for testing.
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
    private OpenApiPaths GetOpenApiPaths(HashSet<OpenApiTag> capturedTags)
    {
        var descriptionsByPath = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(_options.ShouldInclude)
            .GroupBy(apiDescription => apiDescription.MapRelativePathToItemPath());
        var paths = new OpenApiPaths();
        foreach (var descriptions in descriptionsByPath)
        {
            Debug.Assert(descriptions.Key != null, "Relative path mapped to OpenApiPath key cannot be null.");
            paths.Add(descriptions.Key, new OpenApiPathItem { Operations = GetOperations(descriptions, capturedTags) });
        }
        return paths;
    }

    private Dictionary<OperationType, OpenApiOperation> GetOperations(IGrouping<string?, ApiDescription> descriptions, HashSet<OpenApiTag> capturedTags)
    {
        var operations = new Dictionary<OperationType, OpenApiOperation>();
        foreach (var description in descriptions)
        {
            var operation = GetOperation(description, capturedTags);
            operation.Extensions.Add(OpenApiConstants.DescriptionId, new OpenApiString(description.ActionDescriptor.Id));
            _operationTransformerContextCache.TryAdd(description.ActionDescriptor.Id, new OpenApiOperationTransformerContext
            {
                DocumentName = documentName,
                Description = description,
                ApplicationServices = serviceProvider,
            });
            operations[description.GetOperationType()] = operation;
        }
        return operations;
    }

    private static OpenApiOperation GetOperation(ApiDescription description, HashSet<OpenApiTag> capturedTags)
    {
        var tags = GetTags(description);
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                capturedTags.Add(tag);
            }
        }
        var operation = new OpenApiOperation
        {
            Summary = GetSummary(description),
            Description = GetDescription(description),
            Tags = tags,
        };
        return operation;
    }

    private static string? GetSummary(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;

    private static string? GetDescription(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;

    private static List<OpenApiTag>? GetTags(ApiDescription description)
    {
        var actionDescriptor = description.ActionDescriptor;
        if (actionDescriptor.EndpointMetadata?.OfType<ITagsMetadata>().LastOrDefault() is { } tagsMetadata)
        {
            return tagsMetadata.Tags.Select(tag => new OpenApiTag { Name = tag }).ToList();
        }
        // If no tags are specified, use the controller name as the tag. This effectively
        // allows us to group endpoints by the "resource" concept (e.g. users, todos, etc.)
        return [new OpenApiTag { Name = description.ActionDescriptor.RouteValues["controller"] }];
    }
}
