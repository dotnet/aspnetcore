// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A context abstraction for a hub.
    /// </summary>
    public interface IHubContext<THub, T>
        where THub : Hub<T>
        where T : class
    {
        /// <summary>
        /// Gets a <see cref="IHubClients{T}"/> that can be used to invoke methods on clients connected to the hub.
        /// </summary>
        IHubClients<T> Clients { get; }

        /// <summary>
        /// Gets a <see cref="IGroupManager"/> that can be used to add and remove connections to named groups.
        /// </summary>
        IGroupManager Groups { get; }
    }
}
