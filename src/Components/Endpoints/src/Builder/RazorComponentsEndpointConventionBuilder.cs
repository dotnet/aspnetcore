// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
/// </summary>
public sealed class RazorComponentsEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lock;
    private readonly RazorComponentDataSourceOptions _options;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal RazorComponentsEndpointConventionBuilder(
        object @lock,
        ComponentApplicationBuilder builder,
        IEndpointRouteBuilder endpointRouteBuilder,
        RazorComponentDataSourceOptions options,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions)
    {
        _lock = @lock;
        ApplicationBuilder = builder;
        EndpointRouteBuilder = endpointRouteBuilder;
        _options = options;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    /// <summary>
    /// Gets the <see cref="ComponentApplicationBuilder"/> that is used to build the endpoints.
    /// </summary>
    internal ComponentApplicationBuilder ApplicationBuilder { get; }

    internal string? ManifestPath { get => _options.ManifestPath; set => _options.ManifestPath = value; }

    internal bool ResourceCollectionConventionRegistered { get; set; }

    internal IEndpointRouteBuilder EndpointRouteBuilder { get; }

    internal event Action<RazorComponentEndpointUpdateContext>? BeforeCreateEndpoints;

    /// <inheritdoc/>
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        // The lock is shared with the data source. We want to lock here
        // to avoid mutating this list while its read in the data source.
        lock (_lock)
        {
            _conventions.Add(convention);
        }
    }

    /// <inheritdoc/>
    public void Finally(Action<EndpointBuilder> finallyConvention)
    {
        // The lock is shared with the data source. We want to lock here
        // to avoid mutating this list while its read in the data source.
        lock (_lock)
        {
            _finallyConventions.Add(finallyConvention);
        }
    }

    internal void AddRenderMode(IComponentRenderMode renderMode)
    {
        _options.ConfiguredRenderModes.Add(renderMode);
    }

    internal void OnBeforeCreateEndpoints(RazorComponentEndpointUpdateContext endpointContext) =>
        BeforeCreateEndpoints?.Invoke(endpointContext);
}

internal class RazorComponentEndpointUpdateContext(
    List<Endpoint> endpoints,
    RazorComponentDataSourceOptions options)
{
    public List<Endpoint> Endpoints { get; } = endpoints;

    public RazorComponentDataSourceOptions Options { get; } = options;
}

