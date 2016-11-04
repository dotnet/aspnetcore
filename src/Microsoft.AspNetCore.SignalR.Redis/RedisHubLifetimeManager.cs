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
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
    {
        private readonly ConnectionList _connections = new ConnectionList();
        // TODO: Investigate "memory leak" entries never get removed
        private readonly ConcurrentDictionary<string, GroupData> _groups = new ConcurrentDictionary<string, GroupData>();
        private readonly InvocationAdapterRegistry _registry;
        private readonly ConnectionMultiplexer _redisServerConnection;
        private readonly ISubscriber _bus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly RedisOptions _options;

        public RedisHubLifetimeManager(InvocationAdapterRegistry registry,
                                       ILoggerFactory loggerFactory,
                                       IOptions<RedisOptions> options)
        {
            _loggerFactory = loggerFactory;
            _registry = registry;
            _options = options.Value;

            var writer = new LoggerTextWriter(loggerFactory.CreateLogger<RedisHubLifetimeManager<THub>>());
            _redisServerConnection = _options.Connect(writer);
            _bus = _redisServerConnection.GetSubscriber();

            _bus.Subscribe(typeof(THub).FullName, (c, data) =>
            {
                var tasks = new List<Task>(_connections.Count);

                // TODO: serialize once per format by providing a different stream?
                foreach (var connection in _connections)
                {
                    tasks.Add(connection.Channel.Output.WriteAsync((byte[])data));
                }

                // TODO: Task Queue
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            });
        }

        public override Task InvokeAllAsync(string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub).FullName, message);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub).FullName + "." + connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub).FullName + "." + groupName, message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, params object[] args)
        {
            var message = new InvocationDescriptor
            {
                Method = methodName,
                Arguments = args
            };

            return PublishAsync(typeof(THub).FullName + "." + userId, message);
        }

        private async Task PublishAsync(string channel, InvocationDescriptor message)
        {
            // TODO: What format??
            var invocationAdapter = _registry.GetInvocationAdapter("json");

            // BAD
            using (var ms = new MemoryStream())
            {
                await invocationAdapter.WriteInvocationDescriptorAsync(message, ms);

                await _bus.PublishAsync(channel, ms.ToArray());
            }
        }

        public override Task OnConnectedAsync(Connection connection)
        {
            _connections.Add(connection);

            var connectionChannel = typeof(THub).FullName + "." + connection.ConnectionId;
            var userChannel = typeof(THub).FullName + "." + connection.User.Identity.Name;

            var task1 = _bus.SubscribeAsync(connectionChannel, (c, data) =>
            {
                // TODO: serialize once per format by providing a different stream?
                // TODO: Task Queue
                connection.Channel.Output.WriteAsync((byte[])data).GetAwaiter().GetResult();
            });

            var task2 = _bus.SubscribeAsync(userChannel, (c, data) =>
            {
                // TODO: serialize once per format by providing a different stream?
                // TODO: Task Queue
                // TODO: Look at optimizing (looping over connections checking for Name)
                connection.Channel.Output.WriteAsync((byte[])data).GetAwaiter().GetResult();
            });

            var redisSubscriptions = connection.Metadata.GetOrAdd("redis_subscriptions", _ => new HashSet<string>());
            redisSubscriptions.Add(connectionChannel);
            redisSubscriptions.Add(userChannel);

            return Task.WhenAll(task1, task2);
        }

        public override async Task OnDisconnectedAsync(Connection connection)
        {
            _connections.Remove(connection);

            var redisSubscriptions = connection.Metadata.Get<HashSet<string>>("redis_subscriptions");
            if (redisSubscriptions != null)
            {
                foreach (var subscription in redisSubscriptions)
                {
                    await _bus.UnsubscribeAsync(subscription);
                }
            }

            var groupNames = connection.Metadata.Get<HashSet<string>>("group");

            if (groupNames != null)
            {
                foreach (var group in groupNames)
                {
                    await RemoveGroupAsync(connection, group);
                }
            }
        }

        public override async Task AddGroupAsync(Connection connection, string groupName)
        {
            var groupChannel = typeof(THub).FullName + "." + groupName;

            var groupNames = connection.Metadata.GetOrAdd("group", _ => new HashSet<string>());
            groupNames.Add(groupName);

            var group = _groups.GetOrAdd(groupChannel, _ => new GroupData());

            await group.Lock.WaitAsync();
            try
            {
                group.Connections.Add(connection);

                // Subscribe once
                if (group.Connections.Count > 1)
                {
                    return;
                }

                await _bus.SubscribeAsync(groupChannel, (c, data) =>
                {
                    foreach (var groupConnection in group.Connections)
                    {
                        // TODO: serialize once per format by providing a different stream?
                        // TODO: Task Queue
                        groupConnection.Channel.Output.WriteAsync((byte[])data).GetAwaiter().GetResult();
                    }
                });
            }
            finally
            {
                group.Lock.Release();
            }
        }

        public override async Task RemoveGroupAsync(Connection connection, string groupName)
        {
            var groupChannel = typeof(THub).FullName + "." + groupName;

            GroupData group;
            if (!_groups.TryGetValue(groupChannel, out group))
            {
                return;
            }

            var groupNames = connection.Metadata.Get<HashSet<string>>("group");
            groupNames?.Remove(groupName);

            await group.Lock.WaitAsync();
            try
            {
                group.Connections.Remove(connection);

                if (group.Connections.Count == 0)
                {
                    await _bus.UnsubscribeAsync(groupChannel);
                }
            }
            finally
            {
                group.Lock.Release();
            }
        }

        public void Dispose()
        {
            _bus.UnsubscribeAll();
            _redisServerConnection.Dispose();
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

        private class GroupData
        {
            public SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
            public ConnectionList Connections = new ConnectionList();
        }
    }
}
