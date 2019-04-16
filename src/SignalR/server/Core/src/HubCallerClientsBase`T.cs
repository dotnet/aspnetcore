using System;
using System.Collections.Generic;
using System.Text;

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
