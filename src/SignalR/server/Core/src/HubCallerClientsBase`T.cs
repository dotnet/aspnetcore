// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubCallerClientsBase<T> : HubClientsBase<T>
    {
        /// <summary>
        /// Gets a caller to the connection which triggered the current invocation.
        /// </summary>
        public abstract T Caller { get; }

        /// <summary>
        /// Gets a caller to all connections except the one which triggered the current invocation.
        /// </summary>
        public abstract T Others { get; }

        /// <summary>
        /// Gets a caller to all connections in the specified group, except the one which triggered the current invocation.
        /// </summary>
        /// <returns>A client caller.</returns>
        public abstract T OthersInGroup(string groupName);
    }
}
