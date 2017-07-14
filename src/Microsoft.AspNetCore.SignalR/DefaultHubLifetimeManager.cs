// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private long _nextInvocationId = 0;
        private readonly HubConnectionList _connections = new HubConnectionList();

        public override Task AddGroupAsync(string connectionId, string groupName)
        {
            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            var feature = connection.Features.Get<IHubGroupsFeature>();
            var groups = feature.Groups;

            lock (groups)
            {
                groups.Add(groupName);
            }

            return Task.CompletedTask;
        }

        public override Task RemoveGroupAsync(string connectionId, string groupName)
        {
            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            var feature = connection.Features.Get<IHubGroupsFeature>();
            var groups = feature.Groups;

            lock (groups)
            {
                groups.Remove(groupName);
            }

            return Task.CompletedTask;
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, c => true);
        }

        private Task InvokeAllWhere(string methodName, object[] args, Func<HubConnectionContext, bool> include)
        {
            var tasks = new List<Task>(_connections.Count);
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            // TODO: serialize once per format by providing a different stream?
            foreach (var connection in _connections)
            {
                if (!include(connection))
                {
                    continue;
                }

                tasks.Add(WriteAsync(connection, message));
            }

            return Task.WhenAll(tasks);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            var connection = _connections[connectionId];

            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return WriteAsync(connection, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                var feature = connection.Features.Get<IHubGroupsFeature>();
                var groups = feature.Groups;

                // PERF: ...
                lock (groups)
                {
                    return groups.Contains(groupName) == true;
                }
            });
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                return string.Equals(connection.User.Identity.Name, userId, StringComparison.Ordinal);
            });
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            // Set the hub groups feature
            connection.Features.Set<IHubGroupsFeature>(new HubGroupsFeature());

            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        private async Task WriteAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            var payload = connection.Protocol.WriteToArray(hubMessage);

            while (await connection.Output.WaitToWriteAsync())
            {
                if (connection.Output.TryWrite(payload))
                {
                    break;
                }
            }
        }

        private string GetInvocationId()
        {
            var invocationId = Interlocked.Increment(ref _nextInvocationId);
            return invocationId.ToString();
        }

        private interface IHubGroupsFeature
        {
            HashSet<string> Groups { get; }
        }

        private class HubGroupsFeature : IHubGroupsFeature
        {
            public HashSet<string> Groups { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
