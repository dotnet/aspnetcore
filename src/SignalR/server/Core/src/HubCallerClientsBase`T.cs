// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
#pragma warning disable CS0618 // Type or member is obsolete
    public abstract class HubCallerClientsBase<T> : IHubCallerClients<T>
    {
        /// <summary>
        /// Gets a caller to the connection which triggered the current invocation.
        /// </summary>
        public abstract T Caller { get; }

        /// <summary>
        /// Gets a caller to all connections except the one which triggered the current invocation.
        /// </summary>
        public abstract T Others { get; }

        public abstract T All { get; }

        public abstract T AllExcept(IReadOnlyList<string> excludedConnectionIds);

        public abstract T Client(string connectionId);

        public abstract T Clients(IReadOnlyList<string> connectionIds);

        public abstract T Group(string groupName);

        public abstract T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);

        public abstract T Groups(IReadOnlyList<string> groupNames);

        public abstract T OthersInGroup(string groupName);

        public abstract T User(string userId);

        public abstract T Users(IReadOnlyList<string> userIds);
    }
}
