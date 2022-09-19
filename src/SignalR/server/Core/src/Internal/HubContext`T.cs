// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubContext<THub, T> : IHubContext<THub, T>
    where THub : Hub<T>
    where T : class
{
    private readonly HubLifetimeManager<THub> _lifetimeManager;
    private readonly IHubClients<T> _clients;

    public HubContext(HubLifetimeManager<THub> lifetimeManager)
    {
        _lifetimeManager = lifetimeManager;
        _clients = new HubClients<THub, T>(_lifetimeManager);
        Groups = new GroupManager<THub>(lifetimeManager);
    }

    public IHubClients<T> Clients => _clients;

    public IGroupManager Groups { get; }
}
