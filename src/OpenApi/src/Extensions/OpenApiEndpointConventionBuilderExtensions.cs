// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for annotating OpenAPI descriptions on an <see cref="Endpoint" />.
/// </summary>
public static class OpenApiEndpointConventionBuilderExtensions
{
    private const string TrimWarningMessage = "Calls Microsoft.AspNetCore.OpenApi.OpenApiGenerator.GetOpenApiOperation(MethodInfo, EndpointMetadataCollection, RoutePattern) which uses dynamic analysis. Use IServiceCollection.AddOpenApi() to generate OpenAPI metadata at startup for all endpoints,";

    /// <summary>
    /// Adds an OpenAPI annotation to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint.
    /// </summary>
    /// <remarks>
    /// This method does not integrate with built-in OpenAPI document generation support in ASP.NET Core
    /// and is primarily intended for consumption along-side Swashbuckle.AspNetCore.
    /// </remarks>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresDynamicCode(TrimWarningMessage)]
    [RequiresUnreferencedCode(TrimWarningMessage)]
    public static TBuilder WithOpenApi<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Finally(builder => AddAndConfigureOperationForEndpoint(builder));
        return builder;
    }

    /// <summary>
    /// Adds an OpenAPI annotation to <see cref="Endpoint.Metadata" /> associated
    /// with the current endpoint and modifies it with the given <paramref name="configureOperation"/>.
    /// </summary>
    /// <remarks>
    /// This method does not integrate with built-in OpenAPI document generation support in ASP.NET Core
    /// and is primarily intended for consumption along-side Swashbuckle.AspNetCore.
    /// </remarks>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="configureOperation">An <see cref="Func{T, TResult}"/> that returns a new OpenAPI annotation given a generated operation.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresDynamicCode(TrimWarningMessage)]
    [RequiresUnreferencedCode(TrimWarningMessage)]
    public static TBuilder WithOpenApi<TBuilder>(this TBuilder builder, Func<OpenApiOperation, OpenApiOperation> configureOperation)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Finally(endpointBuilder => AddAndConfigureOperationForEndpoint(endpointBuilder, configureOperation));
        return builder;
    }

    [RequiresDynamicCode(TrimWarningMessage)]
    [RequiresUnreferencedCode(TrimWarningMessage)]
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

        if (methodInfo is null)
        {
            return;
        }

        var applicationServices = routeEndpointBuilder.ApplicationServices;
        var hostEnvironment = applicationServices.GetService<IHostEnvironment>();
        var serviceProviderIsService = applicationServices.GetService<IServiceProviderIsService>();
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
