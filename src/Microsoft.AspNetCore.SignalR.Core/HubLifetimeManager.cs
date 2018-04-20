// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubLifetimeManager<THub> where THub : Hub
    {
        // Called by the framework and not something we'd cancel, so it doesn't take a cancellation token
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        // Called by the framework and not something we'd cancel, so it doesn't take a cancellation token
        public abstract Task OnDisconnectedAsync(HubConnectionContext connection);

        public abstract Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default);

        public abstract Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default);

        public abstract Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default);

        public abstract Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);

        public abstract Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
    }

}
