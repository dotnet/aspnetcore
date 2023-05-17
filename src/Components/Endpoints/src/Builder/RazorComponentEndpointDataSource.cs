// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RazorComponentEndpointDataSource<TRootComponent> : EndpointDataSource
{
    private readonly object _lock = new();
    private readonly List<Action<EndpointBuilder>> _conventions = new();
    private readonly List<Action<EndpointBuilder>> _finallyConventions = new();
    private readonly List<IComponentRenderMode> _renderModes = new();
    private readonly ComponentApplicationBuilder _builder;
    private readonly IApplicationBuilder _applicationBuilder;
    private readonly RenderModeEndpointProvider[] _renderModeEndpointProviders;
    private readonly RazorComponentEndpointFactory _factory;

    private List<Endpoint>? _endpoints;
    // TODO: Implement endpoint data source updates https://github.com/dotnet/aspnetcore/issues/47026
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IChangeToken _changeToken;

    public RazorComponentEndpointDataSource(
        ComponentApplicationBuilder builder,
        IEnumerable<RenderModeEndpointProvider> renderModeEndpointProviders,
        IApplicationBuilder applicationBuilder,
        RazorComponentEndpointFactory factory)
    {
        _builder = builder;
        _applicationBuilder = applicationBuilder;
        _renderModeEndpointProviders = renderModeEndpointProviders.ToArray();
        _factory = factory;
        DefaultBuilder = new RazorComponentEndpointConventionBuilder(
            _lock,
            builder,
            _renderModes,
            _conventions,
            _finallyConventions);

        _cancellationTokenSource = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
    }

    internal RazorComponentEndpointConventionBuilder DefaultBuilder { get; }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            // Note it is important that this is lazy, since we only want to create the endpoints after the user had a chance to populate
            // the list of conventions.
            // The order is as follows:
            // * MapRazorComponents gets called and the data source gets created.
            // * The RazorComponentEndpointConventionBuilder is returned and the user gets a chance to call on it to add conventions.
            // * The first request arrives and the DfaMatcherBuilder acesses the data sources to get the endpoints.
            // * The endpoints get created and the conventions get applied.
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _endpoints;
        }
    }

    private void Initialize()
    {
        if (_endpoints == null)
        {
            lock (_lock)
            {
                if (_endpoints == null)
                {
                    UpdateEndpoints();
                }
            }
        }
    }

    private void UpdateEndpoints()
    {
        var endpoints = new List<Endpoint>();
        var context = _builder.Build();
        if (_renderModes.Count == 0)
        {
            _renderModes.AddRange(context.ResolveRenderModes());
        }

        foreach (var definition in context.Pages)
        {
            _factory.AddEndpoints(endpoints, typeof(TRootComponent), definition, _conventions, _finallyConventions);
        }

        var found = false;
        for (var i = 0; i < _renderModes.Count; i++)
        {
            var renderMode = _renderModes[i];
            foreach (var provider in _renderModeEndpointProviders)
            {
                if (provider.Supports(renderMode))
                {
                    found = true;
                    RenderModeEndpointProvider.AddEndpoints(
                        endpoints,
                        typeof(TRootComponent),
                        provider.GetEndpointBuilders(renderMode, _applicationBuilder.New()),
                        renderMode,
                        _conventions,
                        _finallyConventions);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException($"Unable to find a provider for the render mode: {renderMode.GetType().FullName}. This generally" +
                    $"means that a call to 'AddWebAssemblyComponents' or 'AddServerComponents' is missing.");
            }
        }

        _endpoints = endpoints;
    }

    public override IChangeToken GetChangeToken()
    {
        // TODO: Handle updates if necessary (for hot reload).
        return _changeToken;
    }
}
