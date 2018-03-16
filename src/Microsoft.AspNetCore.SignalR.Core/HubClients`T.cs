// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubClients<THub, T> : IHubClients<T> where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public HubClients(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
            All = TypedClientBuilder<T>.Build(new AllClientProxy<THub>(_lifetimeManager));
        }

        public T All { get; }

        public T AllExcept(IReadOnlyList<string> excludedIds)
        {
            return TypedClientBuilder<T>.Build(new AllClientsExceptProxy<THub>(_lifetimeManager, excludedIds));
        }

        public virtual T Client(string connectionId)
        {
            return TypedClientBuilder<T>.Build(new SingleClientProxy<THub>(_lifetimeManager, connectionId));
        }

        public T Clients(IReadOnlyList<string> connectionIds)
        {
            return TypedClientBuilder<T>.Build(new MultipleClientProxy<THub>(_lifetimeManager, connectionIds));
        }

        public virtual T Group(string groupName)
        {
            return TypedClientBuilder<T>.Build(new GroupProxy<THub>(_lifetimeManager, groupName));
        }

        public T GroupExcept(string groupName, IReadOnlyList<string> excludeIds)
        {
            return TypedClientBuilder<T>.Build(new GroupExceptProxy<THub>(_lifetimeManager, groupName, excludeIds));
        }

        public T Groups(IReadOnlyList<string> groupNames)
        {
            return TypedClientBuilder<T>.Build(new MultipleGroupProxy<THub>(_lifetimeManager, groupNames));
        }

        public virtual T User(string userId)
        {
            return TypedClientBuilder<T>.Build(new UserProxy<THub>(_lifetimeManager, userId));
        }

        public virtual T Users(IReadOnlyList<string> userIds)
        {
            return TypedClientBuilder<T>.Build(new MultipleUserProxy<THub>(_lifetimeManager, userIds));
        }
    }
}
