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

// TODO: This will have APIs to add and remove entire assemblies from the list of considered endpoints
// as well as adding/removing individual pages as endpoints.
public class RazorComponentEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lock;
    private readonly ComponentApplicationBuilder _builder;
    private readonly ConfiguredRenderModes _renderModes;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal RazorComponentEndpointConventionBuilder(
        object @lock,
        ComponentApplicationBuilder builder,
        ConfiguredRenderModes renderModes,
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
    /// Explicitly configures the <see cref="IComponentRenderMode"/> available for this application.
    /// </summary>
    /// <param name="renderModes">The list of <see cref="IComponentRenderMode"/> available.</param>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public RazorComponentEndpointConventionBuilder SetRenderModes(params IComponentRenderMode[] renderModes)
    {
        _renderModes.RenderModes ??= new();
        _renderModes.RenderModes.Clear();
        _renderModes.RenderModes.AddRange(renderModes);

        return this;
    }

    /// <summary>
    /// Explicitly configures the <see cref="IComponentRenderMode"/> available for this application to use <see cref="RenderMode.WebAssembly"/>.
    /// </summary>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public RazorComponentEndpointConventionBuilder SetWebAssemblyRenderMode()
    {
        _renderModes.RenderModes ??= new();
        _renderModes.RenderModes.Clear();
        _renderModes.RenderModes.Add(RenderMode.WebAssembly);

        return this;
    }

    /// <summary>
    /// Explicitly configures the <see cref="IComponentRenderMode"/> available for this application to use <see cref="RenderMode.Server"/>.
    /// </summary>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public RazorComponentEndpointConventionBuilder SetServerRenderMode()
    {
        _renderModes.RenderModes ??= new();
        _renderModes.RenderModes.Clear();
        _renderModes.RenderModes.Add(RenderMode.Server);

        return this;
    }

    /// <summary>
    /// Explicitly configures the <see cref="IComponentRenderMode"/> to render exclusively statically.
    /// </summary>
    /// <returns>The <see cref="RazorComponentEndpointConventionBuilder"/>.</returns>
    public RazorComponentEndpointConventionBuilder SetStaticRenderMode()
    {
        _renderModes.RenderModes ??= new();
        _renderModes.RenderModes.Clear();
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
