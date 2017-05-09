// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
    {
        private const string RedisSubscriptionsMetadataName = "redis_subscriptions";

        private readonly ConnectionList _connections = new ConnectionList();
        // TODO: Investigate "memory leak" entries never get removed
        private readonly ConcurrentDictionary<string, GroupData> _groups = new ConcurrentDictionary<string, GroupData>();
        private readonly ConnectionMultiplexer _redisServerConnection;
        private readonly ISubscriber _bus;
        private readonly ILogger _logger;
        private readonly RedisOptions _options;

        // This serializer is ONLY use to transmit the data through redis, it has no connection to the serializer used on each connection.
        private readonly JsonSerializer _serializer = new JsonSerializer
        {
            // We need to serialize objects "full-fidelity", even if it is noisy, so we preserve the original types
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.None
        };

        private long _nextInvocationId = 0;

        public RedisHubLifetimeManager(ILogger<RedisHubLifetimeManager<THub>> logger,
                                       IOptions<RedisOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            var writer = new LoggerTextWriter(logger);
            _logger.LogInformation("Connecting to redis endpoints: {endpoints}", string.Join(", ", options.Value.Options.EndPoints.Select(e => EndPointCollection.ToString(e))));
            _redisServerConnection = _options.Connect(writer);
            if (_redisServerConnection.IsConnected)
            {
                _logger.LogInformation("Connected to redis");
            }
            else
            {
                // TODO: We could support reconnecting, like old SignalR does.
                throw new InvalidOperationException("Connection to redis failed.");
            }
            _bus = _redisServerConnection.GetSubscriber();

            var previousBroadcastTask = Task.CompletedTask;

            var channelName = typeof(THub).FullName;
            _logger.LogInformation("Subscribing to channel: {channel}", channelName);
            _bus.Subscribe(channelName, async (c, data) =>
            {
                await previousBroadcastTask;

                _logger.LogTrace("Received message from redis channel {channel}", channelName);

                var message = DeserializeMessage(data);

                // TODO: This isn't going to work when we allow JsonSerializer customization or add Protobuf
                var tasks = new List<Task>(_connections.Count);

                foreach (var connection in _connections)
                {
                    tasks.Add(WriteAsync(connection, message));
                }

                previousBroadcastTask = Task.WhenAll(tasks);
            });
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(typeof(THub).FullName, message);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(typeof(THub).FullName + "." + connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(typeof(THub).FullName + ".group." + groupName, message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(typeof(THub).FullName + ".user." + userId, message);
        }

        private async Task PublishAsync(string channel, HubMessage hubMessage)
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new JsonTextWriter(new StreamWriter(stream)))
            {
                _serializer.Serialize(writer, hubMessage);
                await writer.FlushAsync();
                payload = stream.ToArray();
            }

            _logger.LogTrace("Publishing message to redis channel {channel}", channel);
            await _bus.PublishAsync(channel, payload);
        }

        public override Task OnConnectedAsync(Connection connection)
        {
            var redisSubscriptions = connection.Metadata.GetOrAdd(RedisSubscriptionsMetadataName, _ => new HashSet<string>());
            var connectionTask = Task.CompletedTask;
            var userTask = Task.CompletedTask;

            _connections.Add(connection);

            var connectionChannel = typeof(THub).FullName + "." + connection.ConnectionId;
            redisSubscriptions.Add(connectionChannel);

            var previousConnectionTask = Task.CompletedTask;

            _logger.LogInformation("Subscribing to connection channel: {channel}", connectionChannel);
            connectionTask = _bus.SubscribeAsync(connectionChannel, async (c, data) =>
            {
                await previousConnectionTask;

                var message = DeserializeMessage(data);

                previousConnectionTask = WriteAsync(connection, message);
            });


            if (connection.User.Identity.IsAuthenticated)
            {
                var userChannel = typeof(THub).FullName + ".user." + connection.User.Identity.Name;
                redisSubscriptions.Add(userChannel);

                var previousUserTask = Task.CompletedTask;

                // TODO: Look at optimizing (looping over connections checking for Name)
                userTask = _bus.SubscribeAsync(userChannel, async (c, data) =>
                {
                    await previousUserTask;

                    var message = DeserializeMessage(data);

                    previousUserTask = WriteAsync(connection, message);
                });
            }

            return Task.WhenAll(connectionTask, userTask);
        }

        public override Task OnDisconnectedAsync(Connection connection)
        {
            _connections.Remove(connection);

            var tasks = new List<Task>();

            var redisSubscriptions = connection.Metadata.Get<HashSet<string>>(RedisSubscriptionsMetadataName);
            if (redisSubscriptions != null)
            {
                foreach (var subscription in redisSubscriptions)
                {
                    _logger.LogInformation("Unsubscribing from channel: {channel}", subscription);
                    tasks.Add(_bus.UnsubscribeAsync(subscription));
                }
            }

            var groupNames = connection.Metadata.Get<HashSet<string>>(HubConnectionMetadataNames.Groups);

            if (groupNames != null)
            {
                // Copy the groups to an array here because they get removed from this collection
                // in RemoveGroupAsync
                foreach (var group in groupNames.ToArray())
                {
                    tasks.Add(RemoveGroupAsync(connection, group));
                }
            }

            return Task.WhenAll(tasks);
        }

        public override async Task AddGroupAsync(Connection connection, string groupName)
        {
            var groupChannel = typeof(THub).FullName + ".group." + groupName;

            var groupNames = connection.Metadata.GetOrAdd(HubConnectionMetadataNames.Groups, _ => new HashSet<string>());

            lock (groupNames)
            {
                groupNames.Add(groupName);
            }

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

                var previousTask = Task.CompletedTask;

                _logger.LogInformation("Subscribing to group channel: {channel}", groupChannel);
                await _bus.SubscribeAsync(groupChannel, async (c, data) =>
                {
                    // Since this callback is async, we await the previous task then
                    // before sending the current message. This is because we don't
                    // want to do concurrent writes to the outgoing connections
                    await previousTask;

                    var message = DeserializeMessage(data);

                    var tasks = new List<Task>(group.Connections.Count);
                    foreach (var groupConnection in group.Connections)
                    {
                        tasks.Add(WriteAsync(groupConnection, message));
                    }

                    previousTask = Task.WhenAll(tasks);
                });
            }
            finally
            {
                group.Lock.Release();
            }
        }

        public override async Task RemoveGroupAsync(Connection connection, string groupName)
        {
            var groupChannel = typeof(THub).FullName + ".group." + groupName;

            GroupData group;
            if (!_groups.TryGetValue(groupChannel, out group))
            {
                return;
            }

            var groupNames = connection.Metadata.Get<HashSet<string>>(HubConnectionMetadataNames.Groups);
            if (groupNames != null)
            {
                lock (groupNames)
                {
                    groupNames.Remove(groupName);
                }
            }

            await group.Lock.WaitAsync();
            try
            {
                group.Connections.Remove(connection);

                if (group.Connections.Count == 0)
                {
                    _logger.LogInformation("Unsubscribing from group channel: {channel}", groupChannel);
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

        private async Task WriteAsync(Connection connection, HubMessage hubMessage)
        {
            var protocol = connection.Metadata.Get<IHubProtocol>(HubConnectionMetadataNames.HubProtocol);
            var data = await protocol.WriteToArrayAsync(hubMessage);
            var message = new Message(data, protocol.MessageType, endOfMessage: true);

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

        private HubMessage DeserializeMessage(RedisValue data)
        {
            HubMessage message;
            using (var reader = new JsonTextReader(new StreamReader(new MemoryStream((byte[])data))))
            {
                message = (HubMessage)_serializer.Deserialize(reader);
            }

            return message;
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
