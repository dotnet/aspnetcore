// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubClients<THub> : IHubClients where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy<THub>(_lifetimeManager);
        }

        public IClientProxy All { get; }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return new AllClientsExceptProxy<THub>(_lifetimeManager, excludedConnectionIds);
        }

        public IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy<THub>(_lifetimeManager, connectionId);
        }

        public IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludedConnectionIds);
        }

        public IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return new MultipleClientProxy<THub>(_lifetimeManager, connectionIds);
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return new MultipleGroupProxy<THub>(_lifetimeManager, groupNames);
        }

        public IClientProxy User(string userId)
        {
            return new UserProxy<THub>(_lifetimeManager, userId);
        }

        public IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return new MultipleUserProxy<THub>(_lifetimeManager, userIds);
        }
    }
}
