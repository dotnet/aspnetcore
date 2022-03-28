// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.OpenApi;

public static class OpenApiOperationExtensions
{
    public static OpenApiPathItem GetOpenApiPathItem(MethodInfo methodInfo, EndpointMetadataCollection metadata)
    {
        var httpMethods = metadata.GetMetadata<IHttpMethodMetadata>();
        var pathItem = new OpenApiPathItem();
        foreach (var httpMethod in httpMethods.HttpMethods)
        {
            pathItem.AddOperation(MapHttpMethodToOperationType(httpMethod), new OpenApiOperation()
            {
                OperationId = GetOperationId(methodInfo, metadata),
                Summary = metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary,
                Description = metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description,
                Tags = GetOperationTags(methodInfo, metadata),
                Parameters = GetOpenApiParameters(methodInfo, metadata),
                RequestBody = GetOpenApiRequestBody(methodInfo, metadata),
                Responses = GetOpenApiResponses(methodInfo, metadata)
            });
        }
        return pathItem;
    }

    private static string? GetOperationId(MethodInfo method, EndpointMetadataCollection metadata)
    {
        return metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
    }

    private static OpenApiResponses GetOpenApiResponses(MethodInfo method, EndpointMetadataCollection metadata)
    {
        var responses = new OpenApiResponses();
        var responseType = method.ReturnType;
        if (AwaitableInfo.IsTypeAwaitable(responseType, out var awaitableInfo))
        {
            responseType = awaitableInfo.ResultType;
        }

        if (typeof(IResult).IsAssignableFrom(responseType))
        {
            responseType = typeof(void);
        }

        var responseProviderMetadata = metadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
        var producesResponseMetadata = metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        var errorMetadata = metadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
        var defaultErrorType = errorMetadata?.Type ?? typeof(void);

        foreach (var response in responseProviderMetadata)
        {
            responses.Add(response.StatusCode.ToString(), new OpenApiResponse
            {

            });
        }

        foreach (var response in producesResponseMetadata)
        {
            responses.Add(response.StatusCode.ToString(), new OpenApiResponse
            {

            });
        }

        return responses;
    }

    private static OpenApiRequestBody? GetOpenApiRequestBody(MethodInfo methodInfo, EndpointMetadataCollection metadata)
    {
        var (hasRequestBody, parameter) = HasFormOrBodyParameter(methodInfo.GetParameters());
        var acceptsMetadata = metadata.GetMetadata<IAcceptsMetadata>();

        if (acceptsMetadata is not null)
        {
            if (!hasRequestBody || parameter is null)
            {
                var requestBody = new OpenApiRequestBody()
                {
                    Required = !acceptsMetadata.IsOptional
                };

                foreach (var contentType in acceptsMetadata.ContentTypes)
                {
                    requestBody.Content.Add(contentType, new OpenApiMediaType {
                        Schema = new OpenApiSchema
                        {
                            Type = acceptsMetadata.RequestType
                        }
                    });
                }

                return requestBody;
            }
        }

        var nullabilityContext = new NullabilityInfoContext();
        var nullability = nullabilityContext.Create(parameter);
        var isOptional = parameter.HasDefaultValue || nullability.ReadState != NullabilityState.NotNull;
        return new OpenApiRequestBody
        {
            Required = !isOptional,
            Content = GetOpenApiRequestBodyContent(parameter)
        };

        static IDictionary<string, OpenApiMediaType> GetOpenApiRequestBodyContent(ParameterInfo parameter)
        {
            return new Dictionary<string, OpenApiMediaType>();
        }
    }

    private static (bool, ParameterInfo?) HasFormOrBodyParameter(ParameterInfo[] parameters)
    {
        var hasBodyOrFormParmaeter = false;
        foreach (var parameter in parameters)
        {
            var attributes = parameter.GetCustomAttributes();
            var hasFromBodyAttribute = attributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } fromBodyAttribute;
            var hasFromFormAttribute = attributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } fromFormAttribute;
            var isFormParameterType = parameter.ParameterType == typeof(IFormFile) || parameter.ParameterType == typeof(IFormFileCollection);
            hasBodyOrFormParmaeter |= hasFromBodyAttribute || hasFromFormAttribute || isFormParameterType;
            if (hasBodyOrFormParmaeter)
            {
                return (true, parameter);
            }
        }
        return (hasBodyOrFormParmaeter, null);
    }

    private static IList<OpenApiTag> GetOperationTags(MethodInfo methodInfo, EndpointMetadataCollection metadata)
    {
        var tags = metadata.GetMetadata<ITagsMetadata>();
        string controllerName = string.Empty;

        if (methodInfo.DeclaringType is not null && !TypeHelper.IsCompilerGeneratedType(methodInfo.DeclaringType))
        {
            controllerName = methodInfo?.DeclaringType?.Name;
        }
        else
        {
            // If the declaring type is null or compiler-generated (e.g. lambdas),
            // group the methods under the application name.
            controllerName = metadata.GetMetadata<IHostEnvironment>()?.ApplicationName ?? string.Empty;
        }

        return tags is not null
            ? tags.Tags.Select(tag => new OpenApiTag() { Name = tag }).ToList()
            : new List<OpenApiTag>() { new OpenApiTag() { Name = controllerName } };
    }

    private static OperationType MapHttpMethodToOperationType(string httpMethod)
    {
        switch (httpMethod)
        {
            case "GET":
                return OperationType.Get;
            case "POST":
                return OperationType.Post;
            default:
                throw new InvalidOperationException($"Cannot create OperationType from {httpMethod}");
        }
    }

    private static IList<OpenApiParameter> GetOpenApiParameters(MethodInfo methodInfo, EndpointMetadataCollection metadata)
    {
        var parameters = methodInfo.GetParameters();
        var openApiParameters = new List<OpenApiParameter>();
        foreach (var parameter in parameters)
        {
            var openApiParameter = new OpenApiParameter()
            {
                Name = parameter.Name,
                In = GetOpenApiParameterLocation(parameter),
                Content = GetOpenApiParameterContent(parameter, metadata)

            };
            openApiParameters.Add(openApiParameter);
        }

        return openApiParameters;
    }

    private static IDictionary<string, OpenApiMediaType> GetOpenApiParameterContent(ParameterInfo parameterInfo, EndpointMetadataCollection metadata)
    {
        var openApiParameterContent = new Dictionary<string, OpenApiMediaType>();
        var acceptsMetadata = metadata.GetMetadata<IAcceptsMetadata>();
        if (acceptsMetadata is not null)
        {
            foreach (var contentType in acceptsMetadata.ContentTypes)
            {
                openApiParameterContent.Add(contentType, new OpenApiMediaType());
            }    
        }

        return openApiParameterContent;
    }

    private static ParameterLocation? GetOpenApiParameterLocation(ParameterInfo parameter)
    {
        var attributes = parameter.GetCustomAttributes();

        if (attributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
        {
            return ParameterLocation.Path;
        }
        else if (attributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            return ParameterLocation.Query;
        }
        else if (attributes.OfType<IFromHeaderMetadata>().FirstOrDefault() is { } headerAttribute)
        {
            return ParameterLocation.Header;
        }
        else if (attributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } fromBodyAttribute)
        {
            return null;
        }
        else if (attributes.OfType<IFromFormMetadata>().FirstOrDefault() is { } fromFormAttribute)
        {
            return null;
        }

        return null;
    }

}
