// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.TestObjects;

public class DynamicEndpointDataSource : EndpointDataSource
{
    private readonly List<Endpoint> _endpoints;
    private CancellationTokenSource _cts;
    private CancellationChangeToken _changeToken;
    private readonly object _lock;

    public DynamicEndpointDataSource(params Endpoint[] endpoints)
    {
        _endpoints = new List<Endpoint>();
        _endpoints.AddRange(endpoints);
        _lock = new object();

        CreateChangeToken();
    }

    public override IChangeToken GetChangeToken() => _changeToken;

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    // Trigger change
    public void AddEndpoint(Endpoint endpoint)
    {
        _endpoints.Add(endpoint);

        // Capture the old tokens so that we can raise the callbacks on them. This is important so that
        // consumers do not register callbacks on an inflight event causing a stackoverflow.
        var oldTokenSource = _cts;
        var oldToken = _changeToken;

        CreateChangeToken();

        // Raise consumer callbacks. Any new callback registration would happen on the new token
        // created in earlier step.
        oldTokenSource.Cancel();
    }

    private void CreateChangeToken()
    {
        _cts = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cts.Token);
    }
}
