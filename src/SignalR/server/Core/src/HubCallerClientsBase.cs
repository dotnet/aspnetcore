// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
#pragma warning disable CS0618 // Type or member is obsolete
    public abstract class HubCallerClientsBase : IHubCallerClients<IClientProxy>
    {
        public abstract IClientProxy Caller { get; }
        public abstract IClientProxy Others { get; }
        public abstract IClientProxy All { get; }
        public abstract IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds);
        public abstract IClientProxy Client(string connectionId);
        public abstract IClientProxy Clients(IReadOnlyList<string> connectionIds);
        public abstract IClientProxy Group(string groupName);
        public abstract IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);
        public abstract IClientProxy Groups(IReadOnlyList<string> groupNames);
        public abstract IClientProxy OthersInGroup(string groupName);
        public abstract IClientProxy User(string userId);
        public abstract IClientProxy Users(IReadOnlyList<string> userIds);
    }
}
