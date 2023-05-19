// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

    private List<Endpoint>? _endpoints;
    // TODO: Implement endpoint data source updates https://github.com/dotnet/aspnetcore/issues/47026
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IChangeToken _changeToken;

    public RazorComponentEndpointDataSource(
        ComponentApplicationBuilder builder,
        RazorComponentEndpointFactory factory)
    {
        _builder = builder;
        _factory = factory;
        DefaultBuilder = new RazorComponentEndpointConventionBuilder(
            _lock,
            builder,
            _conventions,
            _finallyConventions);

        _cancellationTokenSource = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
    }

    private readonly ComponentApplicationBuilder _builder;
    private readonly RazorComponentEndpointFactory _factory;

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
        foreach (var definition in context.Pages)
        {
            _factory.AddEndpoints(endpoints, typeof(TRootComponent), definition, _conventions, _finallyConventions);
        }

        _endpoints = endpoints;
    }

    public override IChangeToken GetChangeToken()
    {
        // TODO: Handle updates if necessary (for hot reload).
        return _changeToken;
    }
}
