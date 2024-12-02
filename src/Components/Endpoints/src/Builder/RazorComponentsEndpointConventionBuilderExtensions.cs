// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            builder.ApplicationBuilder.AddAssembly(assembly);
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
}
