// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubLifetimeManager<THub> where THub : Hub
    {
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        public abstract Task OnDisconnectedAsync(HubConnectionContext connection);

        public abstract Task SendAllAsync(string methodName, object[] args);

        public abstract Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds);

        public abstract Task SendConnectionAsync(string connectionId, string methodName, object[] args);

        public abstract Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args);

        public abstract Task SendGroupAsync(string groupName, string methodName, object[] args);

        public abstract Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args);

        public abstract Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds);

        public abstract Task SendUserAsync(string userId, string methodName, object[] args);

        public abstract Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args);

        public abstract Task AddToGroupAsync(string connectionId, string groupName);

        public abstract Task RemoveFromGroupAsync(string connectionId, string groupName);
    }

}
