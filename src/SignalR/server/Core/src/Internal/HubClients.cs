// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubClients<THub> : HubClientBase where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = new AllClientProxy<THub>(_lifetimeManager);
        }

        public override IClientProxy All { get; }

        public override IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return new AllClientsExceptProxy<THub>(_lifetimeManager, excludedConnectionIds);
        }

        public override IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy<THub>(_lifetimeManager, connectionId);
        }

        public override IClientProxy Group(string groupName)
        {
            return new GroupProxy<THub>(_lifetimeManager, groupName);
        }

        public override IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludedConnectionIds);
        }

        public override IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return new MultipleClientProxy<THub>(_lifetimeManager, connectionIds);
        }

        public override IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return new MultipleGroupProxy<THub>(_lifetimeManager, groupNames);
        }

        public override IClientProxy User(string userId)
        {
            return new UserProxy<THub>(_lifetimeManager, userId);
        }

        public override IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return new MultipleUserProxy<THub>(_lifetimeManager, userIds);
        }
    }
}
