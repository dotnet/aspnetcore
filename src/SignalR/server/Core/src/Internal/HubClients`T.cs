// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class HubClients<THub, T> : HubClientsBase<T> where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = TypedClientBuilder<T>.Build(new AllClientProxy<THub>(_lifetimeManager));
        }

        public override T All { get; }

        public override T AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return TypedClientBuilder<T>.Build(new AllClientsExceptProxy<THub>(_lifetimeManager, excludedConnectionIds));
        }

        public override T Client(string connectionId)
        {
            return TypedClientBuilder<T>.Build(new SingleClientProxy<THub>(_lifetimeManager, connectionId));
        }

        public override T Clients(IReadOnlyList<string> connectionIds)
        {
            return TypedClientBuilder<T>.Build(new MultipleClientProxy<THub>(_lifetimeManager, connectionIds));
        }

        public override T Group(string groupName)
        {
            return TypedClientBuilder<T>.Build(new GroupProxy<THub>(_lifetimeManager, groupName));
        }

        public override T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return TypedClientBuilder<T>.Build(new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludedConnectionIds));
        }

        public override T Groups(IReadOnlyList<string> groupNames)
        {
            return TypedClientBuilder<T>.Build(new MultipleGroupProxy<THub>(_lifetimeManager, groupNames));
        }

        public override T User(string userId)
        {
            return TypedClientBuilder<T>.Build(new UserProxy<THub>(_lifetimeManager, userId));
        }

        public override T Users(IReadOnlyList<string> userIds)
        {
            return TypedClientBuilder<T>.Build(new MultipleUserProxy<THub>(_lifetimeManager, userIds));
        }
    }
}
