// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing.Patterns;
using System.Text;
using Json.Schema;

/// <summary>
/// Service for generating OpenAPI document.
/// </summary>
internal class OpenApiDocumentService
{
    /// <summary>
    /// Gets the OpenAPI document.
    /// </summary>
    public OpenApiDocument Document { get; }

    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
    private readonly OpenApiComponentService _openApiComponentService;
    private readonly IServer _server;
    private readonly HashSet<string> _capturedTags = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiDocumentService"/> class.
    /// </summary>
    /// <param name="apiDescriptionGroupCollectionProvider"></param>
    /// <param name="openApiComponentService"></param>
    /// <param name="server"></param>
    public OpenApiDocumentService(IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider, OpenApiComponentService openApiComponentService, IServer server)
    {
        _apiDescriptionGroupCollectionProvider = apiDescriptionGroupCollectionProvider;
        _openApiComponentService = openApiComponentService;
        _server = server;
        Document = GenerateOpenApiDocument();
    }

    internal OpenApiDocument GenerateOpenApiDocument()
    {
        var document = new OpenApiDocument
        {
            Info = GetOpenApiInfo(),
            Servers = GetOpenApiServers(),
            Paths = GetOpenApiPaths(),
            Components = _openApiComponentService.GetOpenApiComponents(),
            Tags = _capturedTags.Select(tag => new OpenApiTag { Name = tag }).ToList()
        };
        return document;
    }

    private static OpenApiInfo GetOpenApiInfo()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName().Name;
        return new OpenApiInfo
        {
            Title = assemblyName,
            Version = "1.0"
        };
    }

    private IList<OpenApiServer> GetOpenApiServers()
    {
        IList<OpenApiServer> servers = [];
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        foreach (var address in addresses)
        {
            servers.Add(new OpenApiServer { Url = address });
        }
        return servers;
    }

    private OpenApiPaths GetOpenApiPaths()
    {
        var descriptionsByPath = _apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .GroupBy(apiDesc => apiDesc.RelativePath);
        var paths = new OpenApiPaths();
        foreach (var descriptions in descriptionsByPath)
        {
            Debug.Assert(descriptions.Key != null, "Relative paths cannot be null.");
            var path = GetPathRoute(descriptions.Key);
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }
            paths.Add(path, new OpenApiPathItem { Operations = GetOperations(descriptions) });
        }
        return paths;
    }

    private Dictionary<OperationType, OpenApiOperation> GetOperations(IEnumerable<ApiDescription> descriptions)
    {
        var descriptionsByMethod = descriptions.GroupBy(description => description.HttpMethod);
        var operations = new Dictionary<OperationType, OpenApiOperation>();
        foreach (var item in descriptionsByMethod)
        {
            var httpMethod = item.Key;
            var description = item.SingleOrDefault();
            if (description == null)
            {
                continue;
            }
            if (description.ActionDescriptor.EndpointMetadata.OfType<ExcludeFromDescriptionAttribute>().Any())
            {
                continue;
            }
            var tags = GetTags(description);
            foreach (var tag in tags ?? [])
            {
                _capturedTags.Add(tag.Name);
            }
            var operation = new OpenApiOperation
            {
                Tags = tags,
                Summary = GetSummary(description),
                Description = GetDescription(description),
                OperationId = GetOperationId(description),
                RequestBody = GetRequestBody(description),
                Responses = GetResponses(description),
                Parameters = GetParameters(description)
            };
            if (description.ActionDescriptor.EndpointMetadata.OfType<OpenApiOperation>().SingleOrDefault() is OpenApiOperation openApiOperation)
            {
                operations.Add(httpMethod.ToOperationType(), openApiOperation);
            }
            else
            {
                operations.Add(httpMethod.ToOperationType(), operation);
            }
        }
        return operations;
    }

    private static string? GetSummary(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<EndpointSummaryAttribute>().SingleOrDefault()?.Summary;

    private static string? GetDescription(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<EndpointDescriptionAttribute>().SingleOrDefault()?.Description;

    private static string? GetOperationId(ApiDescription description) =>
        description.ActionDescriptor.AttributeRouteInfo?.Name
        ?? (description.ActionDescriptor.EndpointMetadata?.LastOrDefault(m => m is IEndpointNameMetadata) as IEndpointNameMetadata)?.EndpointName
        ?? GenerateOperationId(description);

    private static string GenerateOperationId(ApiDescription description)
    {
        var method = description.HttpMethod;
        var relativePath = description.RelativePath?.Replace("/", "_").Replace('-', '_');
        return method?.ToLowerInvariant() + "_" + relativePath?.Replace("{", string.Empty).Replace("}", string.Empty);
    }

    private static IList<OpenApiTag>? GetTags(ApiDescription description)
    {
        var actionDescriptor = description.ActionDescriptor;
        if (actionDescriptor.EndpointMetadata?.LastOrDefault(m => m is ITagsMetadata) is ITagsMetadata tagsMetadata)
        {
            return tagsMetadata.Tags.Select(tag => new OpenApiTag { Name = tag }).ToList();
        }
        return [new OpenApiTag { Name = description.ActionDescriptor.RouteValues["controller"] }];
    }

    private OpenApiResponses GetResponses(ApiDescription description)
    {
        var supportedResponseTypes = description.SupportedResponseTypes.DefaultIfEmpty(new ApiResponseType { StatusCode = 200 });

        var responses = new OpenApiResponses();
        foreach (var responseType in supportedResponseTypes)
        {
            var statusCode = responseType.IsDefaultResponse ? StatusCodes.Status200OK : responseType.StatusCode;
            responses.Add(statusCode.ToString(CultureInfo.InvariantCulture), GetResponse(description, statusCode, responseType));
        }
        return responses;
    }

    private OpenApiResponse GetResponse(ApiDescription apiDescription, int statusCode, ApiResponseType apiResponseType)
    {
        var description = ReasonPhrases.GetReasonPhrase(statusCode);

        IEnumerable<string> responseContentTypes = [];

        var explicitContentTypes = apiDescription.ActionDescriptor.EndpointMetadata.OfType<ProducesAttribute>().SelectMany(attr => attr.ContentTypes).Distinct();
        if (explicitContentTypes.Any())
        {
            responseContentTypes = explicitContentTypes;
        }

        var apiExplorerContentTypes = apiResponseType.ApiResponseFormats
            .Select(responseFormat => responseFormat.MediaType)
            .Distinct();
        if (apiExplorerContentTypes.Any())
        {
            responseContentTypes = apiExplorerContentTypes;
        }

        return new OpenApiResponse
        {
            Description = description,
            Content = responseContentTypes.ToDictionary(
                contentType => contentType,
                contentType => new OpenApiMediaType { Schema = _openApiComponentService.GetOrCreateJsonSchemaForType(apiResponseType.Type!).Build() }
            )
        };
    }

    private OpenApiRequestBody? GetRequestBody(ApiDescription description)
    {
        if (description.ParameterDescriptions.Any(parameter => parameter.Source == BindingSource.Form || parameter.Source == BindingSource.FormFile))
        {
            return GetFormRequestBody(description);
        }
        return GetJsonRequestBody(description);
    }

    private OpenApiRequestBody? GetFormRequestBody(ApiDescription description)
    {
        var supportedRequestFormats = description.SupportedRequestFormats;
        if (supportedRequestFormats.Count == 0)
        {
            supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/x-www-form-urlencoded" }];
        }

        var contentTypes = supportedRequestFormats.Select(requestFormat => requestFormat.MediaType).Distinct();
        // When multiple form parameters are present, use `AllOf` to denote that the form
        // body should contain schemas for all of the denoted fields.
        var schemas = description.ParameterDescriptions.Where(parameter => parameter.Source == BindingSource.Form || parameter.Source == BindingSource.FormFile)
            .Select(parameter => _openApiComponentService.GetOrCreateJsonSchemaForType(parameter.Type, parameter).Build())
            .ToList();
        var requestBody = new OpenApiRequestBody
        {
            Required = description.ParameterDescriptions.Any(parameter => parameter.Source == BindingSource.Form || parameter.Source == BindingSource.FormFile),
            Content = contentTypes.ToDictionary(
                contentType => contentType,
                contentType => new OpenApiMediaType
                {
                    Schema = schemas is { Count: 1 } ? schemas.First() : new JsonSchemaBuilder().AllOf(schemas).Build()
                }
            )
        };
        return requestBody;
    }

    private OpenApiRequestBody? GetJsonRequestBody(ApiDescription description)
    {
        var supportedRequestFormats = description.SupportedRequestFormats;
        if (supportedRequestFormats.Count == 0)
        {
            supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/json" }];
        }

        var targetParameter = description.ParameterDescriptions.SingleOrDefault(parameter => parameter.Source == BindingSource.Body);
        if (targetParameter == null)
        {
            return null;
        }
        var contentTypes = supportedRequestFormats.Select(requestFormat => requestFormat.MediaType).Distinct();
        var requestBody = new OpenApiRequestBody
        {
            Required = description.ParameterDescriptions.Any(parameter => parameter.Source == BindingSource.Body),
            Content = contentTypes.ToDictionary(
                contentType => contentType,
                contentType => new OpenApiMediaType
                {
                    Schema = _openApiComponentService.GetOrCreateJsonSchemaForType(targetParameter.Type, targetParameter)
                }
            )
        };
        return requestBody;
    }

    private List<OpenApiParameter> GetParameters(ApiDescription description)
    {
        return description.ParameterDescriptions
                .Where(parameter =>
                    parameter.Source != BindingSource.Body && parameter.Source != BindingSource.Form && parameter.Source != BindingSource.FormFile)
                .Select(GetParameter)
                .ToList();
    }

    private OpenApiParameter GetParameter(ApiParameterDescription parameterDescription)
    {
        var parameter = new OpenApiParameter
        {
            Name = parameterDescription.Name,
            In = parameterDescription.Source.ToParameterLocation(),
            Required = parameterDescription.Source == BindingSource.Path || parameterDescription.IsRequired,
            Schema = _openApiComponentService.GetOrCreateJsonSchemaForType(parameterDescription.Type, parameterDescription)
        };
        return parameter;
    }

    private static string GetPathRoute(string path)
    {
        var strippedRoute = new StringBuilder();
        var routePattern = RoutePatternFactory.Parse(path);
        foreach (var segment in routePattern.PathSegments)
        {
            foreach (var part in segment.Parts)
            {
                if (part is RoutePatternLiteralPart literalPart)
                {
                    strippedRoute.Append(literalPart.Content);
                }
                else if (part is RoutePatternParameterPart parameterPart)
                {
                    strippedRoute.Append('{');
                    strippedRoute.Append(parameterPart.Name);
                    strippedRoute.Append('}');
                }
            }
            strippedRoute.Append('/');
        }
        return strippedRoute.ToString();
    }
}
