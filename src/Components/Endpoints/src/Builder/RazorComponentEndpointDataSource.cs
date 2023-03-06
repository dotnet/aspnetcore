// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder;

internal class RazorComponentEndpointDataSource : EndpointDataSource
{
    private readonly object _lock = new object();
    private readonly List<Action<EndpointBuilder>> _conventions = new();

    private List<Endpoint>? _endpoints;
    // TODO: Implement endpoint data source updates https://github.com/dotnet/aspnetcore/issues/47026
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IChangeToken _changeToken;

    public RazorComponentEndpointDataSource()
    {
        DefaultBuilder = new RazorComponentEndpointConventionBuilder(_lock, _conventions);

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
        // TODO: https://github.com/dotnet/aspnetcore/issues/46980

        var entryPoint = Assembly.GetEntryAssembly() ??
            throw new InvalidOperationException("Can't find entry assembly.");

        var pages = entryPoint.GetExportedTypes()
            .Select(t => (type: t, route: t.GetCustomAttribute<RouteAttribute>()))
            .Where(p => p.route != null);

        var endpoints = new List<Endpoint>();
        foreach (var (type, route) in pages)
        {
            // TODO: Proper endpoint definition https://github.com/dotnet/aspnetcore/issues/46985
            var endpoint = new RouteEndpoint(
                CreateRouteDelegate(type),
                RoutePatternFactory.Parse(route!.Template),
                order: 0,
                new EndpointMetadataCollection(type.GetCustomAttributes(inherit: true)),
                route.Template);
            endpoints.Add(endpoint);
        }

        _endpoints = endpoints;
    }

    private static RequestDelegate CreateRouteDelegate(Type type)
    {
        // TODO: Proper endpoint implementation https://github.com/dotnet/aspnetcore/issues/46988
        return (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            return ctx.Response.WriteAsync($"""
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="X-UA-Compatible" content="ie=edge">
    <title>{type.FullName}</title>
    <link rel="stylesheet" href="style.css">
  </head>
  <body>
	<p>{type.FullName}</p>
  </body>
</html>
""");
        };
    }

    public override IChangeToken GetChangeToken()
    {
        // TODO: Handle updates if necessary (for hot reload).
        return _changeToken;
    }
}
