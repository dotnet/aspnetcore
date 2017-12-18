// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class DynamicHubClients
    {
        private readonly IHubCallerClients _clients;

        public DynamicHubClients(IHubCallerClients clients)
        {
            _clients = clients;
        }

        public dynamic All => new DynamicClientProxy(_clients.All);
        public dynamic AllExcept(IReadOnlyList<string> excludedIds) => new DynamicClientProxy(_clients.AllExcept(excludedIds));
        public dynamic Caller => new DynamicClientProxy(_clients.Caller);
        public dynamic Client(string connectionId) => new DynamicClientProxy(_clients.Client(connectionId));
        public dynamic Group(string groupName) => new DynamicClientProxy(_clients.Group(groupName));
        public dynamic GroupExcept(string groupName, IReadOnlyList<string> excludedIds) => new DynamicClientProxy(_clients.GroupExcept(groupName, excludedIds));
        public dynamic OthersInGroup(string groupName) => new DynamicClientProxy(_clients.OthersInGroup(groupName));
        public dynamic Others => new DynamicClientProxy(_clients.Others);
        public dynamic User(string userId) => new DynamicClientProxy(_clients.User(userId));
    }
}