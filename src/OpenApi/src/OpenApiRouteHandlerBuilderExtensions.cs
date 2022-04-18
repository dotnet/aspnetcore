// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

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
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder WithOpenApi(this RouteHandlerBuilder builder)
    {
        builder.Add(endpointBuilder =>
        {
            if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder)
            {
                var openApiOperation = GetOperationForEndpoint(routeEndpointBuilder);
                if (openApiOperation != null)
                {
                    routeEndpointBuilder.Metadata.Add(openApiOperation);
                }
            };
        });
        return builder;

    }

    /// <summary>
    /// Adds an OpenAPI annotation to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint and modifies it with the given <paramref name="configureOperation"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="configureOperation">An <see cref="Func{T, TResult}"/> that returns a new OpenAPI annotation given a generated operation.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder WithOpenApi(this RouteHandlerBuilder builder, Func<OpenApiOperation, OpenApiOperation> configureOperation)
    {
        builder.Add(endpointBuilder =>
        {
            if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder)
            {
                var openApiOperation = GetOperationForEndpoint(routeEndpointBuilder);
                if (openApiOperation != null)
                {
                    routeEndpointBuilder.Metadata.Add(configureOperation(openApiOperation));
                }

            };
        });
        return builder;
    }

    private static OpenApiOperation? GetOperationForEndpoint(RouteEndpointBuilder routeEndpointBuilder)
    {
        var pattern = routeEndpointBuilder.RoutePattern;
        var metadata = new EndpointMetadataCollection(routeEndpointBuilder.Metadata);
        var methodInfo = metadata.OfType<MethodInfo>().SingleOrDefault();
        var serviceProvider = routeEndpointBuilder.ServiceProvider;

        if (methodInfo == null || serviceProvider == null)
        {
            return null;
        }

        var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
        var serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
        var generator = new OpenApiGenerator(hostEnvironment, serviceProviderIsService);
        return generator.GetOpenApiOperation(methodInfo, metadata, pattern);
    }
}
