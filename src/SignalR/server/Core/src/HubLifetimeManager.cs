// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A lifetime manager abstraction for <see cref="Hub"/> instances.
    /// </summary>
    public abstract class HubLifetimeManager<THub> where THub : Hub
    {
        // Called by the framework and not something we'd cancel, so it doesn't take a cancellation token
        /// <summary>
        /// Called when a connection is started.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous connect.</returns>
        public abstract Task OnConnectedAsync(HubConnectionContext connection);

        // Called by the framework and not something we'd cancel, so it doesn't take a cancellation token
        /// <summary>
        /// Called when a connection is finished.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous disconnect.</returns>
        public abstract Task OnDisconnectedAsync(HubConnectionContext connection);

        /// <summary>
        /// Sends an invocation message to all hub connections.
        /// </summary>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendAllAsync(string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to all hub connections excluding the specified connections.
        /// </summary>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendAllExceptAsync(string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendConnectionAsync(string connectionId, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified connections.
        /// </summary>
        /// <param name="connectionIds">The connection IDs.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified group.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendGroupAsync(string groupName, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified groups.
        /// </summary>
        /// <param name="groupNames">The group names.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified group excluding the specified connections.
        /// </summary>
        /// <param name="groupName">The group name.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="excludedConnectionIds">A collection of connection IDs to exclude.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendGroupExceptAsync(string groupName, string methodName, object?[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendUserAsync(string userId, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invocation message to the specified users.
        /// </summary>
        /// <param name="userIds">The user IDs.</param>
        /// <param name="methodName">The invocation method name.</param>
        /// <param name="args">The invocation arguments.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send.</returns>
        public abstract Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a connection to the specified group.
        /// </summary>
        /// <param name="connectionId">The connection ID to add to a group.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous add.</returns>
        public abstract Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a connection from the specified group.
        /// </summary>
        /// <param name="connectionId">The connection ID to remove from a group.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous remove.</returns>
        public abstract Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
    }
}
