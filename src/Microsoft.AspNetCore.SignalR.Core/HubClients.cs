// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubClients<THub> : IHubClients
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy<THub>(_lifetimeManager);
        }

        public IClientProxy All { get; }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedIds)
        {
            return new AllClientsExceptProxy<THub>(_lifetimeManager, excludedIds);
        }

        public IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy<THub>(_lifetimeManager, connectionId);
        }

        public IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludeIds)
        {
            return new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludeIds);
        }

        public IClientProxy MultipleClients(IReadOnlyList<string> connectionIds)
        {
            return new MultipleClientProxy<THub>(_lifetimeManager, connectionIds);
        }

        public IClientProxy User(string userId)
        {
            return new UserProxy<THub>(_lifetimeManager, userId);
        }
    }
}
