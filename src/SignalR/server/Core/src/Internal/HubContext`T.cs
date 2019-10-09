// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Internal
{
	internal class HubContext<THub, T> : IHubContext<THub, T>
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

        public virtual IGroupManager Groups { get; }
    }
}
