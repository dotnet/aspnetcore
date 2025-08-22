// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints.Infrastructure;

/// <summary>
/// A provider that can register endpoints to support a specific <see cref="IComponentRenderMode"/>.
/// </summary>
public abstract class RenderModeEndpointProvider
{
    /// <summary>
    /// Determines whether this <see cref="RenderModeEndpointProvider"/> supports the specified <paramref name="renderMode"/>.
    /// </summary>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/>.</param>
    /// <returns><c>true</c> if the <see cref="IComponentRenderMode"/> is supported; <c>false</c> otherwise.</returns>
    public abstract bool Supports(IComponentRenderMode renderMode);

    /// <summary>
    /// Gets the endpoints for the specified <paramref name="renderMode"/>.
    /// </summary>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/>.</param>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> used to configure non endpoint aware endpoints.</param>
    /// <returns>The list of endpoints this provider is registering.</returns>
    public abstract IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(
        IComponentRenderMode renderMode,
        IApplicationBuilder applicationBuilder);

    internal static void AddEndpoints(
        List<Endpoint> endpoints,
        [DynamicallyAccessedMembers(Component)] Type rootComponent,
        IEnumerable<RouteEndpointBuilder> renderModeEndpoints,
        IComponentRenderMode renderMode,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions)
    {
        foreach (var builder in renderModeEndpoints)
        {
            builder.Metadata.Add(new RootComponentMetadata(rootComponent));
            builder.Metadata.Add(renderMode);
            foreach (var convention in conventions)
            {
                convention(builder);
            }

            foreach (var finallyConvention in finallyConventions)
            {
                finallyConvention(builder);
            }

            endpoints.Add(builder.Build());
        }
    }
}
