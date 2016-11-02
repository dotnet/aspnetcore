using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SocketsSample.EndPoints.Hubs
{
    public class RedisHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
    {
        private readonly InvocationAdapterRegistry _registry;
        private readonly ConnectionMultiplexer _redis;
        private readonly ISubscriber _bus;
        private readonly ILoggerFactory _loggerFactory;

        public RedisHubLifetimeManager(InvocationAdapterRegistry registry, ILoggerFactory loggerFactory)
        {
            var writer = new LoggerTextWriter(loggerFactory.CreateLogger<RedisHubLifetimeManager<THub>>());
            _loggerFactory = loggerFactory;
            _redis = ConnectionMultiplexer.Connect("localhost", writer);
            _bus = _redis.GetSubscriber();
            _registry = registry;
        }

        public override Task InvokeAll(string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub).Name, message);
        }

        public override Task InvokeConnection(string connectionId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub) + "." + connectionId, message);
        }

        public override Task InvokeGroup(string groupName, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub) + "." + groupName, message);
        }

        public override Task InvokeUser(string userId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub) + "." + userId, message);
        }

        private Task PublishAsync(string channel, InvocationDescriptor message)
        {
            // TODO: What format??
            var invocationAdapter = _registry.GetInvocationAdapter("json");

            // BAD
            using (var ms = new MemoryStream())
            {
                invocationAdapter.WriteInvocationDescriptor(message, ms);

                return _bus.PublishAsync(channel, ms.ToArray());
            }
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

            connection.Metadata.Get<ConnectionMultiplexer>("redis")?.Dispose();

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

        private IDisposable Subscribe(string channel, Connection connection)
        {
            var muxer = connection.Metadata.GetOrAdd("redis", k =>
            {
                var logger = _loggerFactory.CreateLogger("REDIS_" + connection.ConnectionId);
                return ConnectionMultiplexer.Connect("localhost", new LoggerTextWriter(logger));
            });

            var subscriber = muxer.GetSubscriber();

            subscriber.SubscribeAsync(channel, (c, data) =>
            {
                connection.Channel.Output.WriteAsync((byte[])data);
            });

            return new DisposableAction(() =>
            {
                subscriber.Unsubscribe(channel);
            });
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private class DisposableAction : IDisposable
        {
            private Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _action, () => { }).Invoke();
            }
        }

        private class LoggerTextWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LoggerTextWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {

            }

            public override void WriteLine(string value)
            {
                _logger.LogDebug(value);
            }
        }
    }
}
