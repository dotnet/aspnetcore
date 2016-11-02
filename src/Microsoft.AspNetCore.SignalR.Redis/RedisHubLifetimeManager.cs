using System;
using System.IO;
using System.Text;
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

        public override async Task OnConnectedAsync(Connection connection)
        {
            await SubscribeAsync(typeof(THub).Name, connection);
            await SubscribeAsync(typeof(THub).Name + "." + connection.ConnectionId, connection);
            await SubscribeAsync(typeof(THub).Name + "." + connection.User.Identity.Name, connection);
        }

        public override Task OnDisconnectedAsync(Connection connection)
        {
            var redisConnection = connection.Metadata.Get<ConnectionMultiplexer>("redis");

            if (redisConnection == null)
            {
                return Task.CompletedTask;
            }

            redisConnection.GetSubscriber().UnsubscribeAll();
            redisConnection.Close(allowCommandsToComplete: true);

            return Task.CompletedTask;
        }

        public override Task AddGroup(Connection connection, string groupName)
        {
            var key = typeof(THub).Name + "." + groupName;
            return SubscribeAsync(key, connection);
        }

        public override Task RemoveGroup(Connection connection, string groupName)
        {
            var key = typeof(THub) + "." + groupName;
            return UnsubscribeAsync(key, connection);
        }

        private Task SubscribeAsync(string channel, Connection connection)
        {
            var redisConnection = connection.Metadata.GetOrAdd("redis", k =>
            {
                var logger = _loggerFactory.CreateLogger("REDIS_" + connection.ConnectionId);
                // TODO: Async
                return _options.Connect(new LoggerTextWriter(logger));
            });

            var subscriber = redisConnection.GetSubscriber();

            return subscriber.SubscribeAsync(channel, (c, data) =>
            {
                connection.Channel.Output.WriteAsync((byte[])data);
            });
        }

        private Task UnsubscribeAsync(string channel, Connection connection)
        {
            var redisConnection = connection.Metadata.Get<ConnectionMultiplexer>("redis");

            if (redisConnection == null)
            {
                return Task.CompletedTask;
            }

            var subscriber = redisConnection.GetSubscriber();

            return subscriber.UnsubscribeAsync(channel);
        }

        public void Dispose()
        {
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
    }
}
