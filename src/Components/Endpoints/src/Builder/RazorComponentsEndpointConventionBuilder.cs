// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Discovery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
/// </summary>
public sealed class RazorComponentsEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lock;
    private readonly ComponentApplicationBuilder _builder;
    private readonly RazorComponentDataSourceOptions _options;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal RazorComponentsEndpointConventionBuilder(
        object @lock,
        ComponentApplicationBuilder builder,
        RazorComponentDataSourceOptions options,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions)
    {
        _lock = @lock;
        _builder = builder;
        _options = options;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    /// <summary>
    /// Gets the <see cref="ComponentApplicationBuilder"/> that is used to build the endpoints.
    /// </summary>
    internal ComponentApplicationBuilder ApplicationBuilder => _builder;

    /// <summary>
    /// Configures the <see cref="RenderMode.Server"/> for this application.
    /// </summary>
    /// <returns>The <see cref="RazorComponentsEndpointConventionBuilder"/>.</returns>
    public RazorComponentsEndpointConventionBuilder AddServerRenderMode()
    {
        _options.ConfiguredRenderModes.Add(RenderMode.Server);

        return this;
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

    /// <summary>
    /// Adds the given <paramref name="renderMode"/> to the list of configured render modes if not present.
    /// </summary>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/> to add.</param>
    public void AddRenderMode(IComponentRenderMode renderMode)
    {
        _options.ConfiguredRenderModes.Add(renderMode);
    }
}
