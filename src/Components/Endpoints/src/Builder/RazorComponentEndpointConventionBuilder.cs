// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
/// </summary>

// TODO: This will have APIs to add and remove entire assemblies from the list of considered endpoints
// as well as adding/removing individual pages as endpoints.
public class RazorComponentEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lock;
    private readonly ComponentApplicationBuilder _builder;
    private readonly List<IComponentRenderMode> _renderModes;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal RazorComponentEndpointConventionBuilder(
        object @lock,
        ComponentApplicationBuilder builder,
        List<IComponentRenderMode> renderModes,
        List<Action<EndpointBuilder>> conventions,
        List<Action<EndpointBuilder>> finallyConventions)
    {
        _lock = @lock;
        _builder = builder;
        _renderModes = renderModes;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    /// <summary>
    /// Gets the <see cref="ComponentApplicationBuilder"/> that is used to build the endpoints.
    /// </summary>
    public ComponentApplicationBuilder ApplicationBuilder => _builder;

    /// <summary>
    /// Restricts the <see cref="IComponentRenderMode"/> available for these endpoints.
    /// </summary>
    /// <param name="renderModes">The render modes the components rendering from these endpoints are
    /// allowed to use.
    /// </param>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public RazorComponentEndpointConventionBuilder SetRenderModes(params IComponentRenderMode[] renderModes)
    {
        _renderModes.Clear();
        _renderModes.AddRange(renderModes);
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
}
