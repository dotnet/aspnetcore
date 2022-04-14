// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Extension methods for annotating OpenAPI descriptions on an <see cref="Endpoint" />.
/// </summary>
public static class OpenApiRouteHandlerBuilderExtensions
{
    /// <summary>
    /// Adds an OpenAPI annotation to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="configureDescription">An <see cref="Action"/> that mutates an OpenAPI annotation.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder WithApiDescription(this RouteHandlerBuilder builder, Action<OpenApiPathItem>? configureDescription = null)
    {
        builder.Add(endpointBuilder =>
        {
            if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder)
            {
                var pattern = routeEndpointBuilder.RoutePattern;
                var metadata = new EndpointMetadataCollection(routeEndpointBuilder.Metadata);
                var methodInfo = metadata.OfType<MethodInfo>().SingleOrDefault();
                var serviceProvider = routeEndpointBuilder.Metadata.OfType<IServiceProvider>().SingleOrDefault();

                if (methodInfo == null || serviceProvider == null)
                {
                    return;
                }

                var generator = serviceProvider.GetService<OpenApiGenerator>();
                var openApiPathItem = generator?.GetOpenApiPathItem(methodInfo, metadata, pattern);

                if (openApiPathItem is null)
                {
                    return;
                }

                if (configureDescription is not null)
                {
                    configureDescription(openApiPathItem);
                }

                endpointBuilder.Metadata.Add(openApiPathItem);
            };
        });
        return builder;
    }
}
