// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A default in-memory lifetime manager abstraction for <see cref="Hub"/> instances.
    /// </summary>
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly HubGroupList _groups = new HubGroupList();
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHubLifetimeManager{THub}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DefaultHubLifetimeManager(ILogger<DefaultHubLifetimeManager<THub>> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            _groups.Add(connection, groupName);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            _groups.Remove(connectionId, groupName);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, include: null, state: null, cancellationToken);
        }

        private Task SendToAllConnections(string methodName, object[] args, Func<HubConnectionContext, object, bool> include, object state = null, CancellationToken cancellationToken = default)
        {
            List<Task> tasks = null;
            SerializedHubMessage message = null;

            // foreach over HubConnectionStore avoids allocating an enumerator
            foreach (var connection in _connections)
            {
                if (include != null && !include(connection, state))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.WriteAsync(message, cancellationToken);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }

            if (tasks == null)
            {
                return Task.CompletedTask;
            }

            // Some connections are slow
            return Task.WhenAll(tasks);
        }

        // Tasks and message are passed by ref so they can be lazily created inside the method post-filtering,
        // while still being re-usable when sending to multiple groups
        private void SendToGroupConnections(string methodName, object[] args, ConcurrentDictionary<string, HubConnectionContext> connections, Func<HubConnectionContext, object, bool> include, object state, ref List<Task> tasks, ref SerializedHubMessage message, CancellationToken cancellationToken)
        {
            // foreach over ConcurrentDictionary avoids allocating an enumerator
            foreach (var connection in connections)
            {
                if (include != null && !include(connection.Value, state))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.Value.WriteAsync(message, cancellationToken);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }
        }

        /// <inheritdoc />
        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            var connection = _connections[connectionId];

            if (connection == null)
            {
                return Task.CompletedTask;
            }

            // We're sending to a single connection
            // Write message directly to connection without caching it in memory
            var message = CreateInvocationMessage(methodName, args);

            return connection.WriteAsync(message, cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                // Can't optimize for sending to a single connection in a group because
                // group might be modified inbetween checking and sending
                List<Task> tasks = null;
                SerializedHubMessage message = null;
                SendToGroupConnections(methodName, args, group, null, null, ref tasks, ref message, cancellationToken);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            // Each task represents the list of tasks for each of the writes within a group
            List<Task> tasks = null;
            SerializedHubMessage message = null;

            foreach (var groupName in groupNames)
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new InvalidOperationException("Cannot send to an empty group name.");
                }

                var group = _groups[groupName];
                if (group != null)
                {
                    SendToGroupConnections(methodName, args, group, null, null, ref tasks, ref message, cancellationToken);
                }
            }

            if (tasks != null)
            {
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                List<Task> tasks = null;
                SerializedHubMessage message = null;

                SendToGroupConnections(methodName, args, group, (connection, state) => !((IReadOnlyList<string>)state).Contains(connection.ConnectionId), excludedConnectionIds, ref tasks, ref message, cancellationToken);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        private SerializedHubMessage CreateSerializedInvocationMessage(string methodName, object[] args)
        {
            return new SerializedHubMessage(CreateInvocationMessage(methodName, args));
        }

        private HubMessage CreateInvocationMessage(string methodName, object[] args)
        {
            return new InvocationMessage(methodName, args);
        }

        /// <inheritdoc />
        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, (connection, state) => string.Equals(connection.UserIdentifier, (string)state, StringComparison.Ordinal), userId, cancellationToken);
        }

        /// <inheritdoc />
        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            _groups.RemoveDisconnectedConnection(connection.ConnectionId);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, (connection, state) => !((IReadOnlyList<string>)state).Contains(connection.ConnectionId), excludedConnectionIds, cancellationToken);
        }

        /// <inheritdoc />
        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, (connection, state) => ((IReadOnlyList<string>)state).Contains(connection.ConnectionId), connectionIds, cancellationToken);
        }

        /// <inheritdoc />
        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, (connection, state) => ((IReadOnlyList<string>)state).Contains(connection.UserIdentifier), userIds, cancellationToken);
        }
    }
}
