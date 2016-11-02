using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;

namespace SocketsSample.EndPoints.Hubs
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private readonly ConnectionList _connections = new ConnectionList();
        private readonly InvocationAdapterRegistry _registry;

        public DefaultHubLifetimeManager(InvocationAdapterRegistry registry)
        {
            _registry = registry;
        }

        public override void AddGroup(Connection connection, string groupName)
        {
            var groups = connection.Metadata.GetOrAdd("groups", k => new HashSet<string>());

            lock (groups)
            {
                groups.Add(groupName);
            }
        }

        public override void RemoveGroup(Connection connection, string groupName)
        {
            var groups = connection.Metadata.Get<HashSet<string>>("groups");

            lock (groups)
            {
                groups.Remove(groupName);
            }
        }

        public override Task InvokeAll(string methodName, params object[] args)
        {
            return InvokeAllWhere(methodName, args, c => true);
        }

        private Task InvokeAllWhere(string methodName, object[] args, Func<Connection, bool> include)
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

                var invocationAdapter = _registry.GetInvocationAdapter((string)connection.Metadata["formatType"]);

                tasks.Add(invocationAdapter.WriteInvocationDescriptor(message, connection.Channel.GetStream()));
            }

            return Task.WhenAll(tasks);
        }

        public override Task InvokeConnection(string connectionId, string methodName, params object[] args)
        {
            var connection = _connections[connectionId];

            var invocationAdapter = _registry.GetInvocationAdapter((string)connection.Metadata["formatType"]);

            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return invocationAdapter.WriteInvocationDescriptor(message, connection.Channel.GetStream());
        }

        public override Task InvokeGroup(string groupName, string methodName, params object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                var groups = connection.Metadata.Get<HashSet<string>>("groups");
                return groups?.Contains(groupName) == true;
            });
        }

        public override Task InvokeUser(string userId, string methodName, params object[] args)
        {
            return InvokeAllWhere(methodName, args, connection =>
            {
                return connection.User.Identity.Name == userId;
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
    }

}
