// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly HubGroupList _groups = new HubGroupList();
        private readonly ILogger _logger;

        public DefaultHubLifetimeManager(ILogger<DefaultHubLifetimeManager<THub>> logger)
        {
            _logger = logger;
        }

        public override Task AddToGroupAsync(string connectionId, string groupName)
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

        public override Task RemoveFromGroupAsync(string connectionId, string groupName)
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

        public override Task SendAllAsync(string methodName, object[] args)
        {
            return SendToAllConnections(methodName, args, null);
        }

        private Task SendToAllConnections(string methodName, object[] args, Func<HubConnectionContext, bool> include)
        {
            List<Task> tasks = null;
            SerializedHubMessage message = null;

            // foreach over HubConnectionStore avoids allocating an enumerator
            foreach (var connection in _connections)
            {
                if (include != null && !include(connection))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.WriteAsync(message);

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
        private void SendToGroupConnections(string methodName, object[] args, ConcurrentDictionary<string, HubConnectionContext> connections, Func<HubConnectionContext, bool> include, ref List<Task> tasks, ref SerializedHubMessage message)
        {
            // foreach over ConcurrentDictionary avoids allocating an enumerator
            foreach (var connection in connections)
            {
                if (include != null && !include(connection.Value))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.Value.WriteAsync(message);

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

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args)
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

            return connection.WriteAsync(message).AsTask();
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args)
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
                SendToGroupConnections(methodName, args, group, null, ref tasks, ref message);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args)
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
                    SendToGroupConnections(methodName, args, group, null, ref tasks, ref message);
                }
            }

            if (tasks != null)
            {
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
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

                SendToGroupConnections(methodName, args, group, connection => !excludedConnectionIds.Contains(connection.ConnectionId), ref tasks, ref message);

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
            return new InvocationMessage(methodName, null, args);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args)
        {
            return SendToAllConnections(methodName, args, connection => string.Equals(connection.UserIdentifier, userId, StringComparison.Ordinal));
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            _groups.RemoveDisconnectedConnection(connection.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            return SendToAllConnections(methodName, args, connection => !excludedConnectionIds.Contains(connection.ConnectionId));
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args)
        {
            return SendToAllConnections(methodName, args, connection => connectionIds.Contains(connection.ConnectionId));
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args)
        {
            return SendToAllConnections(methodName, args, connection => userIds.Contains(connection.UserIdentifier));
        }
    }
}
