// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubContext<THub> : IHubContext<THub>, IHubClients where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubContext(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy<THub>(_lifetimeManager);
            Groups = new GroupManager<THub>(lifetimeManager);
        }

        public IHubClients Clients => this;

        public virtual IClientProxy All { get; }

        public virtual IGroupManager Groups { get; }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedIds)
        {
            return new AllClientsExceptProxy<THub>(_lifetimeManager, excludedIds);
        }

        public virtual IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy<THub>(_lifetimeManager, connectionId);
        }

        public virtual IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }

        public virtual IClientProxy User(string userId)
        {
            return new UserProxy<THub>(_lifetimeManager, userId);
        }
    }
}
