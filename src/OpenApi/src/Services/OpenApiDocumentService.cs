// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly OpenApiComponentService _componentService = serviceProvider.GetRequiredKeyedService<OpenApiComponentService>(documentName);

    private static readonly OpenApiEncoding _defaultFormEncoding = new OpenApiEncoding { Style = ParameterStyle.Form, Explode = true };

    /// <summary>
    /// Cache of <see cref="OpenApiOperationTransformerContext"/> instances keyed by the
    /// `ApiDescription.ActionDescriptor.Id` of the associated operation. ActionDescriptor IDs
    /// are unique within the lifetime of an application and serve as helpful associators between
    /// operations, API descriptions, and their respective transformer contexts.
    /// </summary>
    private readonly ConcurrentDictionary<string, OpenApiOperationTransformerContext> _operationTransformerContextCache = new();
    private static readonly ApiResponseType _defaultApiResponseType = new ApiResponseType { StatusCode = StatusCodes.Status200OK };

    internal bool TryGetCachedOperationTransformerContext(string descriptionId, [NotNullWhen(true)] out OpenApiOperationTransformerContext? context)
        => _operationTransformerContextCache.TryGetValue(descriptionId, out context);

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
            await transformer.TransformAsync(document, documentTransformerContext, cancellationToken);
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

    private OpenApiOperation GetOperation(ApiDescription description, HashSet<OpenApiTag> capturedTags)
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
            Responses = GetResponses(description),
            Parameters = GetParameters(description),
            RequestBody = GetRequestBody(description),
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

    private OpenApiResponses GetResponses(ApiDescription description)
    {
        // OpenAPI requires that each operation have a response, usually a successful one.
        // if there are no response types defined, we assume a successful 200 OK response
        // with no content by default.
        if (description.SupportedResponseTypes.Count == 0)
        {
            return new OpenApiResponses
            {
                ["200"] = GetResponse(description, StatusCodes.Status200OK, _defaultApiResponseType)
            };
        }

        var responses = new OpenApiResponses();
        foreach (var responseType in description.SupportedResponseTypes)
        {
            // The "default" response type is a special case in OpenAPI used to describe
            // the response for all HTTP status codes that are not explicitly defined
            // for a given operation. This is typically used to describe catch-all scenarios
            // like error responses.
            var responseKey = responseType.IsDefaultResponse
                ? OpenApiConstants.DefaultOpenApiResponseKey
                : responseType.StatusCode.ToString(CultureInfo.InvariantCulture);
            responses.Add(responseKey, GetResponse(description, responseType.StatusCode, responseType));
        }
        return responses;
    }

    private OpenApiResponse GetResponse(ApiDescription apiDescription, int statusCode, ApiResponseType apiResponseType)
    {
        var description = ReasonPhrases.GetReasonPhrase(statusCode);
        var response = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        // ApiResponseFormats aggregates information about the supported response content types
        // from different types of Produces metadata. This is handled by ApiExplorer so looking
        // up values in ApiResponseFormats should provide us a complete set of the information
        // encoded in Produces metadata added via attributes or extension methods.
        var apiResponseFormatContentTypes = apiResponseType.ApiResponseFormats
            .Select(responseFormat => responseFormat.MediaType);
        foreach (var contentType in apiResponseFormatContentTypes)
        {
            var schema = apiResponseType.Type is {} type ? _componentService.GetOrCreateSchema(type) : new OpenApiSchema();
            response.Content[contentType] = new OpenApiMediaType { Schema = schema };
        }

        // MVC's `ProducesAttribute` doesn't implement the produces metadata that the ApiExplorer
        // looks for when generating ApiResponseFormats above so we need to pull the content
        // types defined there separately.
        var explicitContentTypes = apiDescription.ActionDescriptor.EndpointMetadata
            .OfType<ProducesAttribute>()
            .SelectMany(attr => attr.ContentTypes);
        foreach (var contentType in explicitContentTypes)
        {
            response.Content[contentType] = new OpenApiMediaType();
        }

        return response;
    }

    private List<OpenApiParameter>? GetParameters(ApiDescription description)
    {
        List<OpenApiParameter>? parameters = null;
        foreach (var parameter in description.ParameterDescriptions)
        {
            // Parameters that should be in the request body should not be
            // populated in the parameters list.
            if (parameter.IsRequestBodyParameter())
            {
                continue;
            }

            var openApiParameter = new OpenApiParameter
            {
                Name = parameter.Name,
                In = parameter.Source.Id switch
                {
                    "Query" => ParameterLocation.Query,
                    "Header" => ParameterLocation.Header,
                    "Path" => ParameterLocation.Path,
                    _ => throw new InvalidOperationException($"Unsupported parameter source: {parameter.Source.Id}")
                },
                // Per the OpenAPI specification, parameters that are sourced from the path
                // are always required, regardless of the requiredness status of the parameter.
                Required = parameter.Source == BindingSource.Path || parameter.IsRequired,
                Schema = _componentService.GetOrCreateSchema(parameter.Type, parameter),
            };
            parameters ??= [];
            parameters.Add(openApiParameter);
        }
        return parameters;
    }

    private OpenApiRequestBody? GetRequestBody(ApiDescription description)
    {
        // Only one parameter can be bound from the body in each request.
        if (description.TryGetBodyParameter(out var bodyParameter))
        {
            return GetJsonRequestBody(description.SupportedRequestFormats, bodyParameter);
        }
        // If there are no body parameters, check for form parameters.
        // Note: Form parameters and body parameters cannot exist simultaneously
        // in the same endpoint.
        if (description.TryGetFormParameters(out var formParameters))
        {
            return GetFormRequestBody(description.SupportedRequestFormats, formParameters);
        }
        return null;
    }

    private OpenApiRequestBody GetFormRequestBody(IList<ApiRequestFormat> supportedRequestFormats, IEnumerable<ApiParameterDescription> formParameters)
    {
        if (supportedRequestFormats.Count == 0)
        {
            // Assume "application/x-www-form-urlencoded" as the default media type
            // to match the default assumed in IFormFeature.
            supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/x-www-form-urlencoded" }];
        }

        var requestBody = new OpenApiRequestBody
        {
            Required = formParameters.Any(parameter => parameter.IsRequired),
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        // Forms are represented as objects with properties for each form field.
        var schema = new OpenApiSchema { Type = "object", Properties = new Dictionary<string, OpenApiSchema>() };
        foreach (var parameter in formParameters)
        {
            schema.Properties[parameter.Name] = _componentService.GetOrCreateSchema(parameter.Type);
        }

        foreach (var requestFormat in supportedRequestFormats)
        {
            var contentType = requestFormat.MediaType;
            requestBody.Content[contentType] = new OpenApiMediaType
            {
                Schema = schema,
                Encoding = new Dictionary<string, OpenApiEncoding>() { [contentType] = _defaultFormEncoding }
            };
        }

        return requestBody;
    }

    private OpenApiRequestBody GetJsonRequestBody(IList<ApiRequestFormat> supportedRequestFormats, ApiParameterDescription bodyParameter)
    {
        if (supportedRequestFormats.Count == 0)
        {
            if (bodyParameter.Type == typeof(Stream) || bodyParameter.Type == typeof(PipeReader))
            {
                // Assume "application/octet-stream" as the default media type
                // for stream-based parameter types.
                supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/octet-stream" }];
            }
            else
            {
                // Assume "application/json" as the default media type
                // for everything else.
                supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/json" }];
            }
        }

        var requestBody = new OpenApiRequestBody
        {
            Required = bodyParameter.IsRequired,
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        foreach (var requestForm in supportedRequestFormats)
        {
            var contentType = requestForm.MediaType;
            requestBody.Content[contentType] = new OpenApiMediaType { Schema = _componentService.GetOrCreateSchema(bodyParameter.Type) };
        }

        return requestBody;
    }
}
