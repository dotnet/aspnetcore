// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private long _nextInvocationId = 0;
        private readonly ConnectionList _connections = new ConnectionList();

        public override Task AddGroupAsync(Connection connection, string groupName)
        {
            var groups = connection.Metadata.GetOrAdd(HubConnectionMetadataNames.Groups, _ => new HashSet<string>());

            lock (groups)
            {
                groups.Add(groupName);
            }

            return Task.CompletedTask;
        }

        public override Task RemoveGroupAsync(Connection connection, string groupName)
        {
            var groups = connection.Metadata.Get<HashSet<string>>(HubConnectionMetadataNames.Groups);

            if (groups == null)
            {
                return Task.CompletedTask;
            }

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

        private Task InvokeAllWhere(string methodName, object[] args, Func<Connection, bool> include)
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
                var groups = connection.Metadata.Get<HashSet<string>>(HubConnectionMetadataNames.Groups);
                return groups?.Contains(groupName) == true;
            });
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                return string.Equals(connection.User.Identity.Name, userId, StringComparison.Ordinal);
            });
        }

        public override Task OnConnectedAsync(Connection connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Connection connection)
        {
            _connections.Remove(connection);
            return Task.CompletedTask;
        }

        private async Task WriteAsync(Connection connection, HubMessage hubMessage)
        {
            var protocol = connection.Metadata.Get<IHubProtocol>(HubConnectionMetadataNames.HubProtocol);
            var payload = await protocol.WriteToArrayAsync(hubMessage);
            var message = new Message(payload, protocol.MessageType, endOfMessage: true);

            while (await connection.Transport.Output.WaitToWriteAsync())
            {
                if (connection.Transport.Output.TryWrite(message))
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
    }
}
