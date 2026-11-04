// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace IntegrationTestsWebsite.Infrastructure;

/// <summary>
/// This endpoint data source can be modified and will raise a change token event.
/// It can be used to add new endpoints after the application has started.
/// </summary>
public class DynamicEndpointDataSource : EndpointDataSource
{
    private readonly List<Endpoint> _endpoints = new List<Endpoint>();
    private CancellationTokenSource? _cts;
    private CancellationChangeToken? _cct;

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public override IChangeToken GetChangeToken()
    {
        if (_cts == null)
        {
            _cts = new CancellationTokenSource();
        }
        if (_cct == null)
        {
            _cct = new CancellationChangeToken(_cts.Token);
        }

        return _cct;
    }

    public void AddEndpoints(IEnumerable<Endpoint> endpoints)
    {
        // Avoid ambiguous match result when the same URL is used between tests
        foreach (var newEndpoint in endpoints)
        {
            if (newEndpoint is RouteEndpoint routeEndpoint)
            {
                var existingMatch = _endpoints
                    .OfType<RouteEndpoint>()
                    .SingleOrDefault(e => e.RoutePattern.RawText == routeEndpoint.RoutePattern.RawText);
                if (existingMatch != null)
                {
                    _endpoints.Remove(existingMatch);
                }
            }

            _endpoints.Add(newEndpoint);
        }

        if (_cts != null)
        {
            var localCts = _cts;

            _cts = null;
            _cct = null;

            localCts.Cancel();
        }
    }
}
