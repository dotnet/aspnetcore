// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.References;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDocumentService(
    [ServiceKey] string documentName,
    IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
    IHostEnvironment hostEnvironment,
    IOptionsMonitor<OpenApiOptions> optionsMonitor,
    IServiceProvider serviceProvider,
    IServer? server = null)
{
    private readonly OpenApiOptions _options = optionsMonitor.Get(documentName);
    private readonly OpenApiSchemaService _componentService = serviceProvider.GetRequiredKeyedService<OpenApiSchemaService>(documentName);

    /// <summary>
    /// Cache of <see cref="OpenApiOperationTransformerContext"/> instances keyed by the
    /// `ApiDescription.ActionDescriptor.Id` of the associated operation. ActionDescriptor IDs
    /// are unique within the lifetime of an application and serve as helpful associators between
    /// operations, API descriptions, and their respective transformer contexts.
    /// </summary>
    private readonly ConcurrentDictionary<string, OpenApiOperationTransformerContext> _operationTransformerContextCache = new();
    private static readonly ApiResponseType _defaultApiResponseType = new() { StatusCode = StatusCodes.Status200OK };

    private static readonly FrozenSet<string> _disallowedHeaderParameters = new[] { HeaderNames.Accept, HeaderNames.Authorization, HeaderNames.ContentType }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal bool TryGetCachedOperationTransformerContext(string descriptionId, [NotNullWhen(true)] out OpenApiOperationTransformerContext? context)
        => _operationTransformerContextCache.TryGetValue(descriptionId, out context);

    public async Task<OpenApiDocument> GetOpenApiDocumentAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken = default)
    {
        // Schema and operation transformers are scoped per-request and can be
        // pre-allocated to hold the same number of transformers as the associated
        // options object.
        var schemaTransformers = _options.SchemaTransformers.Count > 0
            ? new IOpenApiSchemaTransformer[_options.SchemaTransformers.Count]
            : [];
        var operationTransformers = _options.OperationTransformers.Count > 0 ?
            new IOpenApiOperationTransformer[_options.OperationTransformers.Count]
            : [];
        InitializeTransformers(scopedServiceProvider, schemaTransformers, operationTransformers);
        var document = new OpenApiDocument
        {
            Info = GetOpenApiInfo(),
            Servers = GetOpenApiServers()
        };
        document.Paths = await GetOpenApiPathsAsync(document, scopedServiceProvider, operationTransformers, schemaTransformers, cancellationToken);
        document.Tags = document.Tags?.Distinct(OpenApiTagComparer.Instance).ToList();
        try
        {
            await ApplyTransformersAsync(document, scopedServiceProvider, cancellationToken);
        }

        finally
        {
            await FinalizeTransformers(schemaTransformers, operationTransformers);
        }
        // Call register components to support
        // resolution of references in the document.
        document.Workspace ??= new();
        document.Workspace.RegisterComponents(document);
        if (document.Components?.Schemas is not null)
        {
            document.Components.Schemas = new SortedDictionary<string, OpenApiSchema>(document.Components.Schemas);
        }
        return document;
    }

    private async Task ApplyTransformersAsync(OpenApiDocument document, IServiceProvider scopedServiceProvider, CancellationToken cancellationToken)
    {
        var documentTransformerContext = new OpenApiDocumentTransformerContext
        {
            DocumentName = documentName,
            ApplicationServices = scopedServiceProvider,
            DescriptionGroups = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items,
        };
        // Use index-based for loop to avoid allocating an enumerator with a foreach.
        for (var i = 0; i < _options.DocumentTransformers.Count; i++)
        {
            var transformer = _options.DocumentTransformers[i];
            await transformer.TransformAsync(document, documentTransformerContext, cancellationToken);
        }
    }

    internal void InitializeTransformers(IServiceProvider scopedServiceProvider, IOpenApiSchemaTransformer[] schemaTransformers, IOpenApiOperationTransformer[] operationTransformers)
    {
        for (var i = 0; i < _options.SchemaTransformers.Count; i++)
        {
            var schemaTransformer = _options.SchemaTransformers[i];
            if (schemaTransformer is TypeBasedOpenApiSchemaTransformer typeBasedTransformer)
            {
                schemaTransformers[i] = typeBasedTransformer.InitializeTransformer(scopedServiceProvider);
            }
            else
            {
                schemaTransformers[i] = schemaTransformer;
            }
        }

        for (var i = 0; i < _options.OperationTransformers.Count; i++)
        {
            var operationTransformer = _options.OperationTransformers[i];
            if (operationTransformer is TypeBasedOpenApiOperationTransformer typeBasedTransformer)
            {
                operationTransformers[i] = typeBasedTransformer.InitializeTransformer(scopedServiceProvider);
            }
            else
            {
                operationTransformers[i] = operationTransformer;
            }
        }
    }

    internal static async Task FinalizeTransformers(IOpenApiSchemaTransformer[] schemaTransformers, IOpenApiOperationTransformer[] operationTransformers)
    {
        for (var i = 0; i < schemaTransformers.Length; i++)
        {
            await schemaTransformers[i].FinalizeTransformer();
        }
        for (var i = 0; i < operationTransformers.Length; i++)
        {
            await operationTransformers[i].FinalizeTransformer();
        }
    }

    internal async Task ForEachOperationAsync(
        OpenApiDocument document,
        Func<OpenApiOperation, OpenApiOperationTransformerContext, CancellationToken, Task> callback,
        CancellationToken cancellationToken)
    {
        foreach (var pathItem in document.Paths.Values)
        {
            for (var i = 0; i < OpenApiConstants.OperationTypes.Length; i++)
            {
                var operationType = OpenApiConstants.OperationTypes[i];
                if (!pathItem.Operations.TryGetValue(operationType, out var operation))
                {
                    continue;
                }

                if (operation.Annotations is { } annotations &&
                    annotations.TryGetValue(OpenApiConstants.DescriptionId, out var descriptionId) &&
                    descriptionId is string descriptionIdString &&
                    TryGetCachedOperationTransformerContext(descriptionIdString, out var operationContext))
                {
                    await callback(operation, operationContext, cancellationToken);
                }
                else
                {
                    // If the cached operation transformer context was not found, throw an exception.
                    // This can occur if the `x-aspnetcore-id` extension attribute was removed by the
                    // user in another operation transformer or if the lookup for operation transformer
                    // context resulted in a cache miss. As an alternative here, we could just to implement
                    // the "slow-path" and look up the ApiDescription associated with the OpenApiOperation
                    // using the OperationType and given path, but we'll avoid this for now.
                    throw new InvalidOperationException("Cached operation transformer context not found. Please ensure that the operation contains the `x-aspnetcore-id` extension attribute.");
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

    internal List<OpenApiServer> GetOpenApiServers()
    {
        if (hostEnvironment.IsDevelopment() &&
            server?.Features.Get<IServerAddressesFeature>()?.Addresses is { Count: > 0 } addresses)
        {
            return addresses.Select(address => new OpenApiServer { Url = address }).ToList();
        }
        return [];
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
    private async Task<OpenApiPaths> GetOpenApiPathsAsync(
        OpenApiDocument document,
        IServiceProvider scopedServiceProvider,
        IOpenApiOperationTransformer[] operationTransformers,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        var descriptionsByPath = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(_options.ShouldInclude)
            .GroupBy(apiDescription => apiDescription.MapRelativePathToItemPath());
        var paths = new OpenApiPaths();
        foreach (var descriptions in descriptionsByPath)
        {
            Debug.Assert(descriptions.Key != null, "Relative path mapped to OpenApiPath key cannot be null.");
            paths.Add(descriptions.Key, new OpenApiPathItem { Operations = await GetOperationsAsync(descriptions, document, scopedServiceProvider, operationTransformers, schemaTransformers, cancellationToken) });
        }
        return paths;
    }

    private async Task<Dictionary<OperationType, OpenApiOperation>> GetOperationsAsync(
        IGrouping<string?, ApiDescription> descriptions,
        OpenApiDocument document,
        IServiceProvider scopedServiceProvider,
        IOpenApiOperationTransformer[] operationTransformers,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        var operations = new Dictionary<OperationType, OpenApiOperation>();
        foreach (var description in descriptions)
        {
            var operation = await GetOperationAsync(description, document, scopedServiceProvider, schemaTransformers, cancellationToken);
            operation.Annotations ??= new Dictionary<string, object>();
            operation.Annotations.Add(OpenApiConstants.DescriptionId, description.ActionDescriptor.Id);

            var operationContext = new OpenApiOperationTransformerContext
            {
                DocumentName = documentName,
                Description = description,
                ApplicationServices = scopedServiceProvider,
            };

            _operationTransformerContextCache.TryAdd(description.ActionDescriptor.Id, operationContext);
            operations[description.GetOperationType()] = operation;

            // Use index-based for loop to avoid allocating an enumerator with a foreach.
            for (var i = 0; i < operationTransformers.Length; i++)
            {
                var transformer = operationTransformers[i];
                await transformer.TransformAsync(operation, operationContext, cancellationToken);
            }
        }
        return operations;
    }

    private async Task<OpenApiOperation> GetOperationAsync(
        ApiDescription description,
        OpenApiDocument document,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        var tags = GetTags(description, document);
        var operation = new OpenApiOperation
        {
            OperationId = GetOperationId(description),
            Summary = GetSummary(description),
            Description = GetDescription(description),
            Responses = await GetResponsesAsync(document, description, scopedServiceProvider, schemaTransformers, cancellationToken),
            Parameters = await GetParametersAsync(document, description, scopedServiceProvider, schemaTransformers, cancellationToken),
            RequestBody = await GetRequestBodyAsync(document, description, scopedServiceProvider, schemaTransformers, cancellationToken),
            Tags = tags,
        };
        return operation;
    }

    private static string? GetSummary(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;

    private static string? GetDescription(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;

    private static string? GetOperationId(ApiDescription description)
        => description.ActionDescriptor.AttributeRouteInfo?.Name ??
            description.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().LastOrDefault()?.EndpointName;

    private static List<OpenApiTagReference> GetTags(ApiDescription description, OpenApiDocument document)
    {
        var actionDescriptor = description.ActionDescriptor;
        if (actionDescriptor.EndpointMetadata?.OfType<ITagsMetadata>().LastOrDefault() is { } tagsMetadata)
        {
            List<OpenApiTagReference> tags = [];
            foreach (var tag in tagsMetadata.Tags)
            {
                document.Tags ??= [];
                document.Tags.Add(new OpenApiTag { Name = tag });
                tags.Add(new OpenApiTagReference(tag, document));

            }
            return tags;
        }
        // If no tags are specified, use the controller name as the tag. This effectively
        // allows us to group endpoints by the "resource" concept (e.g. users, todos, etc.)
        var controllerName = description.ActionDescriptor.RouteValues["controller"];
        document.Tags ??= [];
        document.Tags.Add(new OpenApiTag { Name = controllerName });
        return [new OpenApiTagReference(controllerName, document)];
    }

    private async Task<OpenApiResponses> GetResponsesAsync(
        OpenApiDocument document,
        ApiDescription description,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        // OpenAPI requires that each operation have a response, usually a successful one.
        // if there are no response types defined, we assume a successful 200 OK response
        // with no content by default.
        if (description.SupportedResponseTypes.Count == 0)
        {
            return new OpenApiResponses
            {
                ["200"] = await GetResponseAsync(document, description, StatusCodes.Status200OK, _defaultApiResponseType, scopedServiceProvider, schemaTransformers, cancellationToken)
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
            responses.Add(responseKey, await GetResponseAsync(document, description, responseType.StatusCode, responseType, scopedServiceProvider, schemaTransformers, cancellationToken));
        }
        return responses;
    }

    private async Task<OpenApiResponse> GetResponseAsync(
        OpenApiDocument document,
        ApiDescription apiDescription,
        int statusCode,
        ApiResponseType apiResponseType,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        var response = new OpenApiResponse
        {
            Description = apiResponseType.Description ?? ReasonPhrases.GetReasonPhrase(statusCode),
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
            var schema = apiResponseType.Type is { } type ? await _componentService.GetOrCreateSchemaAsync(document, type, scopedServiceProvider, schemaTransformers, null, cancellationToken) : new OpenApiSchema();
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
            response.Content.TryAdd(contentType, new OpenApiMediaType());
        }

        return response;
    }

    private async Task<List<OpenApiParameter>?> GetParametersAsync(
        OpenApiDocument document,
        ApiDescription description,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        List<OpenApiParameter>? parameters = null;
        foreach (var parameter in description.ParameterDescriptions)
        {
            if (ShouldIgnoreParameter(parameter))
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
                    _ => ParameterLocation.Query
                },
                Required = IsRequired(parameter),
                Schema = await _componentService.GetOrCreateSchemaAsync(document, GetTargetType(description, parameter), scopedServiceProvider, schemaTransformers, parameter, cancellationToken: cancellationToken),
                Description = GetParameterDescriptionFromAttribute(parameter)
            };

            parameters ??= [];
            parameters.Add(openApiParameter);
        }

        return parameters;

        static bool ShouldIgnoreParameter(ApiParameterDescription parameter)
        {
            if (parameter.IsRequestBodyParameter())
            {
                // Parameters that should be in the request body should not be
                // populated in the parameters list.
                return true;
            }
            else if (parameter.Source == BindingSource.Header && _disallowedHeaderParameters.Contains(parameter.Name))
            {
                // OpenAPI 3.0 states certain headers are "not allowed" to be defined as parameters.
                // See https://github.com/dotnet/aspnetcore/issues/57305 for more context.
                return true;
            }

            return false;
        }
    }

    private static bool IsRequired(ApiParameterDescription parameter)
    {
        var hasRequiredAttribute = parameter.ParameterDescriptor is IParameterInfoParameterDescriptor parameterInfoDescriptor &&
            parameterInfoDescriptor.ParameterInfo.GetCustomAttributes(inherit: true).Any(attr => attr is RequiredAttribute);
        // Per the OpenAPI specification, parameters that are sourced from the path
        // are always required, regardless of the requiredness status of the parameter.
        return parameter.Source == BindingSource.Path || parameter.IsRequired || hasRequiredAttribute;
    }

    // Apply [Description] attributes on the parameter to the top-level OpenApiParameter object and not the schema.
    private static string? GetParameterDescriptionFromAttribute(ApiParameterDescription parameter) =>
        parameter.ParameterDescriptor is IParameterInfoParameterDescriptor { ParameterInfo: { } parameterInfo } &&
        parameterInfo.GetCustomAttributes().OfType<DescriptionAttribute>().LastOrDefault() is { } descriptionAttribute ?
            descriptionAttribute.Description :
            null;

    private async Task<OpenApiRequestBody?> GetRequestBodyAsync(OpenApiDocument document, ApiDescription description, IServiceProvider scopedServiceProvider, IOpenApiSchemaTransformer[] schemaTransformers, CancellationToken cancellationToken)
    {
        // Only one parameter can be bound from the body in each request.
        if (description.TryGetBodyParameter(out var bodyParameter))
        {
            return await GetJsonRequestBody(document, description.SupportedRequestFormats, bodyParameter, scopedServiceProvider, schemaTransformers, cancellationToken);
        }
        // If there are no body parameters, check for form parameters.
        // Note: Form parameters and body parameters cannot exist simultaneously
        // in the same endpoint.
        if (description.TryGetFormParameters(out var formParameters))
        {
            var endpointMetadata = description.ActionDescriptor.EndpointMetadata;
            return await GetFormRequestBody(document, description.SupportedRequestFormats, formParameters, endpointMetadata, scopedServiceProvider, schemaTransformers, cancellationToken);
        }
        return null;
    }

    private async Task<OpenApiRequestBody> GetFormRequestBody(
        OpenApiDocument document,
        IList<ApiRequestFormat> supportedRequestFormats,
        IEnumerable<ApiParameterDescription> formParameters,
        IList<object> endpointMetadata,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
    {
        if (supportedRequestFormats.Count == 0)
        {
            // Assume "application/x-www-form-urlencoded" as the default media type
            // to match the default assumed in IFormFeature.
            supportedRequestFormats = [new ApiRequestFormat { MediaType = "application/x-www-form-urlencoded" }];
        }

        var requestBody = new OpenApiRequestBody
        {
            // Form bodies are always required because the framework doesn't support
            // serializing a form collection from an empty body. Instead, requiredness
            // must be set on a per-parameter basis. See below.
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        var schema = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = new Dictionary<string, OpenApiSchema>() };
        // Group form parameters by their name because MVC explodes form parameters that are bound from the
        // same model instance into separate ApiParameterDescriptions in ApiExplorer, while minimal APIs does not.
        //
        // public record Todo(int Id, string Title, bool Completed, DateTime CreatedAt)
        // public void PostMvc([FromForm] Todo person) { }
        // app.MapGet("/form-todo", ([FromForm] Todo todo) => Results.Ok(todo));
        //
        // In the example above, MVC's ApiExplorer will bind four separate arguments to the Todo model while minimal APIs will
        // bind a single Todo model instance to the todo parameter. Grouping by name allows us to handle both cases.
        var groupedFormParameters = formParameters.GroupBy(parameter => parameter.ParameterDescriptor.Name);
        // If there is only one real parameter derived from the form body, then set it directly in the schema.
        var hasMultipleFormParameters = groupedFormParameters.Count() > 1;
        foreach (var parameter in groupedFormParameters)
        {
            // ContainerType is not null when the parameter has been exploded into separate API
            // parameters by ApiExplorer as in the MVC model.
            if (parameter.All(parameter => parameter.ModelMetadata.ContainerType is null))
            {
                var description = parameter.Single();
                var parameterSchema = await _componentService.GetOrCreateSchemaAsync(document, description.Type, scopedServiceProvider, schemaTransformers, description, cancellationToken: cancellationToken);
                // Form files are keyed by their parameter name so we must capture the parameter name
                // as a property in the schema.
                if (description.Type == typeof(IFormFile) || description.Type == typeof(IFormFileCollection))
                {
                    if (IsRequired(description))
                    {
                        schema.Required.Add(description.Name);
                    }
                    if (hasMultipleFormParameters)
                    {
                        schema.AllOf.Add(new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                [description.Name] = parameterSchema
                            }
                        });
                    }
                    else
                    {
                        schema.Properties[description.Name] = parameterSchema;
                    }
                }
                else
                {
                    // Resolve complex type state from endpoint metadata when checking for
                    // minimal API types to use trim friendly code paths.
                    var isComplexType = endpointMetadata
                        .OfType<IParameterBindingMetadata>()
                        .SingleOrDefault(parameter => parameter.Name == description.Name)?
                        .HasTryParse == false;
                    if (hasMultipleFormParameters)
                    {
                        // Here and below: POCOs do not need to be need under their parameter name in the grouping.
                        // The form-binding implementation will capture them implicitly.
                        if (isComplexType)
                        {
                            schema.AllOf.Add(parameterSchema);
                        }
                        else
                        {
                            if (IsRequired(description))
                            {
                                schema.Required.Add(description.Name);
                            }
                            schema.AllOf.Add(new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    [description.Name] = parameterSchema
                                }
                            });
                        }
                    }
                    else
                    {
                        if (isComplexType)
                        {
                            schema = parameterSchema;
                        }
                        else
                        {
                            if (IsRequired(description))
                            {
                                schema.Required.Add(description.Name);
                            }
                            schema.Properties[description.Name] = parameterSchema;
                        }
                    }
                }
            }
            else
            {
                if (hasMultipleFormParameters)
                {
                    var propertySchema = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = new Dictionary<string, OpenApiSchema>() };
                    foreach (var description in parameter)
                    {
                        propertySchema.Properties[description.Name] = await _componentService.GetOrCreateSchemaAsync(document, description.Type, scopedServiceProvider, schemaTransformers, description, cancellationToken: cancellationToken);
                    }
                    schema.AllOf.Add(propertySchema);
                }
                else
                {
                    foreach (var description in parameter)
                    {
                        schema.Properties[description.Name] = await _componentService.GetOrCreateSchemaAsync(document, description.Type, scopedServiceProvider, schemaTransformers, description, cancellationToken: cancellationToken);
                    }
                }
            }
        }

        foreach (var requestFormat in supportedRequestFormats)
        {
            var contentType = requestFormat.MediaType;
            requestBody.Content[contentType] = new OpenApiMediaType
            {
                Schema = schema
            };
        }

        return requestBody;
    }

    private async Task<OpenApiRequestBody> GetJsonRequestBody(
        OpenApiDocument document,
        IList<ApiRequestFormat> supportedRequestFormats,
        ApiParameterDescription bodyParameter,
        IServiceProvider scopedServiceProvider,
        IOpenApiSchemaTransformer[] schemaTransformers,
        CancellationToken cancellationToken)
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
            Required = IsRequired(bodyParameter),
            Content = new Dictionary<string, OpenApiMediaType>(),
            Description = GetParameterDescriptionFromAttribute(bodyParameter)
        };

        foreach (var requestForm in supportedRequestFormats)
        {
            var contentType = requestForm.MediaType;
            requestBody.Content[contentType] = new OpenApiMediaType { Schema = await _componentService.GetOrCreateSchemaAsync(document, bodyParameter.Type, scopedServiceProvider, schemaTransformers, bodyParameter, cancellationToken: cancellationToken) };
        }

        return requestBody;
    }

    /// <remarks>
    /// This method is used to determine the target type for a given parameter. The target type
    /// is the actual type that should be used to generate the schema for the parameter. This is
    /// necessary because MVC's ModelMetadata layer will set ApiParameterDescription.Type to string
    /// when the parameter is a parsable or convertible type. In this case, we want to use the actual
    /// model type to generate the schema instead of the string type.
    /// </remarks>
    /// <remarks>
    /// This method will also check if no target type was resolved from the <see cref="ApiParameterDescription"/>
    /// and default to a string schema. This will happen if we are dealing with an inert route parameter
    /// that does not define a specific parameter type in the route handler or in the response.
    /// </remarks>
    private static Type GetTargetType(ApiDescription description, ApiParameterDescription parameter)
    {
        var bindingMetadata = description.ActionDescriptor.EndpointMetadata
            .OfType<IParameterBindingMetadata>()
            .SingleOrDefault(metadata => metadata.Name == parameter.Name);
        var parameterType = parameter.Type is not null
            ? Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type
            : parameter.Type;

        // parameter.Type = typeof(string)
        // parameter.ModelMetadata.Type = typeof(TEnum)
        var requiresModelMetadataFallbackForEnum = parameterType == typeof(string)
            && parameter.ModelMetadata.ModelType != parameter.Type
            && parameter.ModelMetadata.ModelType.IsEnum;
        // Enums are exempt because we want to set the OpenApiSchema.Enum field when feasible.
        // parameter.Type = typeof(TEnum), typeof(TypeWithTryParse)
        // parameter.ModelMetadata.Type = typeof(string)
        var hasTryParse = bindingMetadata?.HasTryParse == true && parameterType is not null && !parameterType.IsEnum;
        var targetType = requiresModelMetadataFallbackForEnum || hasTryParse
            ? parameter.ModelMetadata.ModelType
            : parameter.Type;
        targetType ??= typeof(string);
        return targetType;
    }
}
