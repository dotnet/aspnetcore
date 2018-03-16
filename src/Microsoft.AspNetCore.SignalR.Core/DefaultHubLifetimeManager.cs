// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        private readonly HubConnectionList _connections = new HubConnectionList();
        private readonly HubGroupList _groups = new HubGroupList();
        private readonly ILogger _logger;

        public DefaultHubLifetimeManager(ILogger<DefaultHubLifetimeManager<THub>> logger)
        {
            _logger = logger;
        }

        public override Task AddGroupAsync(string connectionId, string groupName)
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

        public override Task RemoveGroupAsync(string connectionId, string groupName)
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
            return SendAllWhere(methodName, args, c => true);
        }

        private Task SendAllWhere(string methodName, object[] args, Func<HubConnectionContext, bool> include)
        {
            var count = _connections.Count;
            if (count == 0)
            {
                return Task.CompletedTask;
            }

            var tasks = new List<Task>(count);
            var message = CreateInvocationMessage(methodName, args);

            foreach (var connection in _connections)
            {
                if (!include(connection))
                {
                    continue;
                }

                tasks.Add(SafeWriteAsync(connection, message));
            }

            return Task.WhenAll(tasks);
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

            var message = CreateInvocationMessage(methodName, args);

            return SafeWriteAsync(connection, message);
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
                var message = CreateInvocationMessage(methodName, args);
                var tasks = group.Values.Select(c => SafeWriteAsync(c, message));
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args)
        {
            // Each task represents the list of tasks for each of the writes within a group
            var tasks = new List<Task>();
            var message = CreateInvocationMessage(methodName, args);

            foreach (var groupName in groupNames)
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new ArgumentException(nameof(groupName));
                }

                var group = _groups[groupName];
                if (group != null)
                {
                    tasks.Add(Task.WhenAll(group.Values.Select(c =>  SafeWriteAsync(c, message))));
                }
            }

            return Task.WhenAll(tasks);
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                var message = CreateInvocationMessage(methodName, args);
                var tasks = group.Values.Where(connection => !excludedIds.Contains(connection.ConnectionId))
                    .Select(c => SafeWriteAsync(c, message));
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        private InvocationMessage CreateInvocationMessage(string methodName, object[] args)
        {
            return new InvocationMessage(target: methodName, argumentBindingException: null, arguments: args);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args)
        {
            return SendAllWhere(methodName, args, connection =>
                string.Equals(connection.UserIdentifier, userId, StringComparison.Ordinal));
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

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            return SendAllWhere(methodName, args, connection =>
            {
                return !excludedIds.Contains(connection.ConnectionId);
            });
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args)
        {
            return SendAllWhere(methodName, args, connection =>
            {
                return connectionIds.Contains(connection.ConnectionId);
            });
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args)
        {
            return SendAllWhere(methodName, args, connection =>
            {
                return userIds.Contains(connection.UserIdentifier);
            });
        }

        // This method is to protect against connections throwing synchronously when writing to them and preventing other connections from being written to
        private async Task SafeWriteAsync(HubConnectionContext connection, InvocationMessage message)
        {
            try
            {
                await connection.WriteAsync(message);
            }
            // This exception isn't interesting to users
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _failedWritingMessage =
                LoggerMessage.Define(LogLevel.Warning, new EventId(1, "FailedWritingMessage"), "Failed writing message.");

            public static void FailedWritingMessage(ILogger logger, Exception exception)
            {
                _failedWritingMessage(logger, exception);
            }
        }
    }
}
