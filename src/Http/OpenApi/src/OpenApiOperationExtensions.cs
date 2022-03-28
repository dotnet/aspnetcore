// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Mirosoft.AspNetCore.OpenApi;

public static class OpenApiOperationExtensions
{
    public static OpenApiPathItem GetOpenApiPathItem(MethodInfo methodInfo, EndpointMetadataCollection metadata)
    {
        var httpMethods = metadata.GetMetadata<IHttpMethodMetadata>();
        var pathItem = new OpenApiPathItem();
        foreach (var httpMethod in httpMethods.HttpMethods)
        {
            pathItem.AddOperation(OperationType.Get, new OpenApiOperation()
            {

                Summary = metadata.OfType<IEndpointSummaryMetadata>().SingleOrDefault()?.Summary,
                Description = metadata.OfType<IEndpointDescriptionMetadata>().SingleOrDefault()?.Description,
                // Tags = metadata.OfType<ITagsMetadata>().SingleOrDefault()?.Tags,
                Parameters = GetOpenApiParameters(methodInfo)

            });
            }
        return pathItem;
    }

    private static IList<OpenApiParameter> GetOpenApiParameters(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        var openApiParameters = new List<OpenApiParameter>();
        foreach (var parameter in parameters)
        {
            var openApiParameter = new OpenApiParameter()
            {
                Name = parameter.Name,
                In = GetOpenApiParameterLocation(parameter)

            };
            openApiParameters.Add(openApiParameter);
        }
        return openApiParameters;
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
