// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class GroupManager<THub> : IGroupManager where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupManager(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.AddToGroupAsync(connectionId, groupName, cancellationToken);
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
        }
    }
}
