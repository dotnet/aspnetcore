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
    private readonly List<Action<ComponentApplicationBuilder>> _componentApplicationBuilderActions;
    private readonly Action _configurationChanged;

    internal RazorComponentsEndpointConventionBuilder(
        object @lock,
        IEndpointRouteBuilder endpointRouteBuilder,
        RazorComponentDataSourceOptions options,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions,
        List<Action<ComponentApplicationBuilder>> componentApplicationBuilderActions,
        Action configurationChanged)
    {
        _lock = @lock;
        EndpointRouteBuilder = endpointRouteBuilder;
        _options = options;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
        _componentApplicationBuilderActions = componentApplicationBuilderActions;
        _configurationChanged = configurationChanged;
    }

    internal List<Action<ComponentApplicationBuilder>> ComponentApplicationBuilderActions => _componentApplicationBuilderActions;

    internal string? ManifestPath
    {
        get => _options.ManifestPath;
        set
        {
            _options.ManifestPath = value;
            _configurationChanged();
        }
    }

    internal bool ResourceCollectionConventionRegistered { get; set; }

    internal IEndpointRouteBuilder EndpointRouteBuilder { get; }

    internal event Action<RazorComponentEndpointUpdateContext>? BeforeCreateEndpoints;

    internal void AddComponentApplicationBuilderAction(Action<ComponentApplicationBuilder> action)
    {
        lock (_lock)
        {
            _componentApplicationBuilderActions.Add(action);
        }

        _configurationChanged();
    }

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

        _configurationChanged();
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

        _configurationChanged();
    }

    internal void AddRenderMode(IComponentRenderMode renderMode)
    {
        lock (_lock)
        {
            _options.ConfiguredRenderModes.Add(renderMode);
        }

        _configurationChanged();
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

