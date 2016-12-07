// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private readonly ConnectionList<StreamingConnection> _connections = new ConnectionList<StreamingConnection>();
        private readonly InvocationAdapterRegistry _registry;

        public DefaultHubLifetimeManager(InvocationAdapterRegistry registry)
        {
            _registry = registry;
        }

        public override Task AddGroupAsync(StreamingConnection connection, string groupName)
        {
            var groups = connection.Metadata.GetOrAdd("groups", _ => new HashSet<string>());

            lock (groups)
            {
                groups.Add(groupName);
            }

            return TaskCache.CompletedTask;
        }

        public override Task RemoveGroupAsync(StreamingConnection connection, string groupName)
        {
            var groups = connection.Metadata.Get<HashSet<string>>("groups");

            lock (groups)
            {
                groups.Remove(groupName);
            }

            return TaskCache.CompletedTask;
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, c => true);
        }

        private Task InvokeAllWhere(string methodName, object[] args, Func<StreamingConnection, bool> include)
        {
            var tasks = new List<Task>(_connections.Count);
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            // TODO: serialize once per format by providing a different stream?
            foreach (var connection in _connections)
            {
                if (!include(connection))
                {
                    continue;
                }

                var invocationAdapter = _registry.GetInvocationAdapter(connection.Metadata.Get<string>("formatType"));

                tasks.Add(invocationAdapter.WriteMessageAsync(message, connection.Transport.GetStream()));
            }

            return Task.WhenAll(tasks);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            var connection = _connections[connectionId];

            var invocationAdapter = _registry.GetInvocationAdapter(connection.Metadata.Get<string>("formatType"));

            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return invocationAdapter.WriteMessageAsync(message, connection.Transport.GetStream());
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                var groups = connection.Metadata.Get<HashSet<string>>("groups");
                return groups?.Contains(groupName) == true;
            });
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                return connection.User.Identity.Name == userId;
            });
        }

        public override Task OnConnectedAsync(StreamingConnection connection)
        {
            _connections.Add(connection);
            return TaskCache.CompletedTask;
        }

        public override Task OnDisconnectedAsync(StreamingConnection connection)
        {
            _connections.Remove(connection);
            return TaskCache.CompletedTask;
        }
    }

}
