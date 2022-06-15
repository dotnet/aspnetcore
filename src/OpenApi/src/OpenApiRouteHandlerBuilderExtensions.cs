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
    public static TBuilder WithOpenApi<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(builder => AddAndConfigureOperationForEndpoint(builder));
        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI annotation to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint and modifies it with the given <paramref name="configureOperation"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="configureOperation">An <see cref="Func{T, TResult}"/> that returns a new OpenAPI annotation given a generated operation.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static TBuilder WithOpenApi<TBuilder>(this TBuilder builder, Func<OpenApiOperation, OpenApiOperation> configureOperation)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder => AddAndConfigureOperationForEndpoint(endpointBuilder, configureOperation));
        return builder;
    }

    private static void AddAndConfigureOperationForEndpoint(EndpointBuilder endpointBuilder, Func<OpenApiOperation, OpenApiOperation>? configure = null)
    {
        foreach (var item in endpointBuilder.Metadata)
        {
            if (item is OpenApiOperation existingOperation)
            {
                if (configure is not null)
                {
                    var configuredOperation = configure(existingOperation);

                    if (!ReferenceEquals(configuredOperation, existingOperation))
                    {
                        endpointBuilder.Metadata.Remove(existingOperation);

                        // The only way configureOperation could be null here is if configureOperation violated it's signature and returned null.
                        // We could throw or something, removing the previous metadata seems fine.
                        if (configuredOperation is not null)
                        {
                            endpointBuilder.Metadata.Add(configuredOperation);
                        }
                    }
                }

                return;
            }
        }

        // We cannot generate an OpenApiOperation without routeEndpointBuilder.RoutePattern.
        if (endpointBuilder is not RouteEndpointBuilder routeEndpointBuilder)
        {
            return;
        }

        var pattern = routeEndpointBuilder.RoutePattern;
        var metadata = new EndpointMetadataCollection(routeEndpointBuilder.Metadata);
        var methodInfo = metadata.OfType<MethodInfo>().SingleOrDefault();
        var serviceProvider = routeEndpointBuilder.ServiceProvider;

        if (methodInfo == null || serviceProvider == null)
        {
            return;
        }

        var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
        var serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
        var generator = new OpenApiGenerator(hostEnvironment, serviceProviderIsService);
        var newOperation = generator.GetOpenApiOperation(methodInfo, metadata, pattern);

        if (newOperation is not null)
        {
            if (configure is not null)
            {
                newOperation = configure(newOperation);
            }

            if (newOperation is not null)
            {
                routeEndpointBuilder.Metadata.Add(newOperation);
            }
        }
    }
}
