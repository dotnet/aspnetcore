// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Internal;

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
        public dynamic AllExcept(IReadOnlyList<string> excludedConnectionIds) => new DynamicClientProxy(_clients.AllExcept(excludedConnectionIds));
        public dynamic Caller => new DynamicClientProxy(_clients.Caller);
        public dynamic Client(string connectionId) => new DynamicClientProxy(_clients.Client(connectionId));
        public dynamic Clients(IReadOnlyList<string> connectionIds) => new DynamicClientProxy(_clients.Clients(connectionIds));
        public dynamic Group(string groupName) => new DynamicClientProxy(_clients.Group(groupName));
        public dynamic Groups(IReadOnlyList<string> groupNames) => new DynamicClientProxy(_clients.Groups(groupNames));
        public dynamic GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new DynamicClientProxy(_clients.GroupExcept(groupName, excludedConnectionIds));
        public dynamic OthersInGroup(string groupName) => new DynamicClientProxy(_clients.OthersInGroup(groupName));
        public dynamic Others => new DynamicClientProxy(_clients.Others);
        public dynamic User(string userId) => new DynamicClientProxy(_clients.User(userId));
        public dynamic Users(IReadOnlyList<string> users) => new DynamicClientProxy(_clients.Users(users));
    }
}