// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configures which assemblies are part of the given Razor Component Application.
/// </summary>
public static class RazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the given additional assemblies to the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>"/>
    /// <param name="assemblies">The <see cref="Assembly"/> instances to add.</param>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    /// <remarks>
    /// The provided assemblies will be scanned for pages that will be mapped as endpoints.
    /// </remarks>
    public static RazorComponentsEndpointConventionBuilder AddAdditionalAssemblies(
        this RazorComponentsEndpointConventionBuilder builder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            builder.ComponentApplicationBuilderActions.Add(b => b.AddAssembly(assembly));
        }
        return builder;
    }

    /// <summary>
    /// Sets a <see cref="ResourceAssetCollection"/> and <see cref="ImportMapDefinition"/> metadata
    /// for the component application.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>
    /// <param name="manifestPath">The manifest associated with the assets.</param>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    /// <remarks>
    /// The <paramref name="manifestPath"/> must match the path of the manifes file provided to
    /// the <see cref="StaticAssetsEndpointRouteBuilderExtensions.MapStaticAssets(Routing.IEndpointRouteBuilder, string?)"/>
    /// call.
    /// </remarks>
    public static RazorComponentsEndpointConventionBuilder WithStaticAssets(
        this RazorComponentsEndpointConventionBuilder builder,
        string? manifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ManifestPath = manifestPath;
        if (!builder.ResourceCollectionConventionRegistered)
        {
            builder.ResourceCollectionConventionRegistered = true;
            var convention = new ResourceCollectionConvention(new ResourceCollectionResolver(builder.EndpointRouteBuilder));
            builder.BeforeCreateEndpoints += convention.OnBeforeCreateEndpoints;
            builder.Add(convention.ApplyConvention);
        }

        return builder;
    }

    /// <summary>
    /// Configures framework endpoints for the Blazor application. Framework endpoints are internal
    /// infrastructure endpoints such as opaque redirection, disconnect, and initializers.
    /// </summary>
    /// <param name="builder">The <see cref="RazorComponentsEndpointConventionBuilder"/>.</param>
    /// <param name="configureEndpoints">A callback to configure framework endpoints.</param>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    /// <remarks>
    /// This method provides a way to apply specific conventions or metadata to Blazor's infrastructure
    /// endpoints without affecting regular component endpoints. Framework endpoints are identified by
    /// the presence of <see cref="ComponentFrameworkEndpointMetadata"/> in their metadata collection.
    /// </remarks>
    public static RazorComponentsEndpointConventionBuilder ConfigureFrameworkEndpoints(
        this RazorComponentsEndpointConventionBuilder builder,
        Action<EndpointBuilder> configureEndpoints)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureEndpoints);

        builder.Add(endpointBuilder =>
        {
            // Only apply configuration to endpoints that have ComponentFrameworkEndpointMetadata
            if (endpointBuilder.Metadata.OfType<ComponentFrameworkEndpointMetadata>().Any())
            {
                configureEndpoints(endpointBuilder);
            }
        });

        return builder;
    }
}
