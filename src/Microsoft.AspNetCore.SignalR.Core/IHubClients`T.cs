// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// An abstraction that provides access to client connections.
    /// </summary>
    /// <typeparam name="T">The client invoker type.</typeparam>
    public interface IHubClients<T>
    {
        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub.
        /// </summary>
        /// <returns>A client caller.</returns>
        T All { get; }

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified client connections.
        /// </summary>
        /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
        /// <returns>A client caller.</returns>
        T AllExcept(IReadOnlyList<string> excludedConnectionIds);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified client connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <returns>A client caller.</returns>
        T Client(string connectionId);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified client connections.
        /// </summary>
        /// <param name="connectionIds">The connection IDs.</param>
        /// <returns>A client caller.</returns>
        T Clients(IReadOnlyList<string> connectionIds);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <returns>A client caller.</returns>
        T Group(string groupName);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
        /// </summary>
        /// <param name="groupNames">The group names.</param>
        /// <returns>A client caller.</returns>
        T Groups(IReadOnlyList<string> groupNames);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
        /// <returns>A client caller.</returns>
        T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with the specified user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A client caller.</returns>
        T User(string userId);

        /// <summary>
        /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
        /// </summary>
        /// <param name="userIds">The user IDs.</param>
        /// <returns>A client caller.</returns>
        T Users(IReadOnlyList<string> userIds);
    }
}

