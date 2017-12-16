// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubLifetimeManager<THub>
    {
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        public abstract Task OnDisconnectedAsync(HubConnectionContext connection);

        public abstract Task InvokeAllAsync(string methodName, object[] args);

        public abstract Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds);

        public abstract Task InvokeConnectionAsync(string connectionId, string methodName, object[] args);

        public abstract Task InvokeGroupAsync(string groupName, string methodName, object[] args);

        public abstract Task InvokeGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedIds);

        public abstract Task InvokeUserAsync(string userId, string methodName, object[] args);

        public abstract Task AddGroupAsync(string connectionId, string groupName);

        public abstract Task RemoveGroupAsync(string connectionId, string groupName);
    }

}
