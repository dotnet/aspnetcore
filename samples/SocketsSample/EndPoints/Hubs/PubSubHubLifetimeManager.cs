using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using SocketsSample.Hubs;

namespace SocketsSample.EndPoints.Hubs
{
    public class PubSubHubLifetimeManager<THub> : HubLifetimeManager<THub>
    {
        private readonly IPubSub _bus;
        private readonly InvocationAdapterRegistry _registry;

        public PubSubHubLifetimeManager(IPubSub bus, InvocationAdapterRegistry registry)
        {
            _bus = bus;
            _registry = registry;
        }

        public override Task InvokeAll(string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return _bus.Publish(typeof(THub).Name, message);
        }

        public override Task InvokeConnection(string connectionId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return _bus.Publish(typeof(THub) + "." + connectionId, message);
        }

        public override Task InvokeGroup(string groupName, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return _bus.Publish(typeof(THub) + "." + groupName, message);
        }

        public override Task InvokeUser(string userId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return _bus.Publish(typeof(THub) + "." + userId, message);
        }

        public override Task OnConnectedAsync(Connection connection)
        {
            var subs = connection.Metadata.GetOrAdd("subscriptions", k => new List<IDisposable>());

            subs.Add(Subscribe(typeof(THub).Name, connection));
            subs.Add(Subscribe(typeof(THub).Name + "." + connection.ConnectionId, connection));
            subs.Add(Subscribe(typeof(THub).Name + "." + connection.User.Identity.Name, connection));

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Connection connection)
        {
            var subs = connection.Metadata.Get<IList<IDisposable>>("subscriptions");

            if (subs != null)
            {
                foreach (var sub in subs)
                {
                    sub.Dispose();
                }
            }

            return Task.CompletedTask;
        }

        public override void AddGroup(Connection connection, string groupName)
        {
            var groups = connection.Metadata.GetOrAdd("groups", k => new ConcurrentDictionary<string, IDisposable>());
            var key = typeof(THub).Name + "." + groupName;
            groups.TryAdd(key, Subscribe(key, connection));
        }

        public override void RemoveGroup(Connection connection, string groupName)
        {
            var key = typeof(THub) + "." + groupName;
            var groups = connection.Metadata.Get<ConcurrentDictionary<string, IDisposable>>("groups");

            IDisposable subscription;
            if (groups != null && groups.TryRemove(key, out subscription))
            {
                subscription.Dispose();
            }
        }

        private IDisposable Subscribe(string signal, Connection connection)
        {
            return _bus.Subscribe(signal, message =>
            {
                var invocationAdapter = _registry.GetInvocationAdapter((string)connection.Metadata["formatType"]);

                return invocationAdapter.WriteInvocationDescriptor((InvocationDescriptor)message, connection.Channel.GetStream());
            });
        }
    }

}
