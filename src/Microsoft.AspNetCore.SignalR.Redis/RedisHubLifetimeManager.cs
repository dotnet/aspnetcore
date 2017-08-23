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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
    {
        private readonly HubConnectionList _connections = new HubConnectionList();
        // TODO: Investigate "memory leak" entries never get removed
        private readonly ConcurrentDictionary<string, GroupData> _groups = new ConcurrentDictionary<string, GroupData>();
        private readonly ConnectionMultiplexer _redisServerConnection;
        private readonly ISubscriber _bus;
        private readonly ILogger _logger;
        private readonly RedisOptions _options;
        private readonly string _channelNamePrefix = typeof(THub).FullName;

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

            var channelName = _channelNamePrefix;
            _logger.LogInformation("Subscribing to channel: {channel}", channelName);
            _bus.Subscribe(channelName, async (c, data) =>
            {
                await previousBroadcastTask;

                _logger.LogTrace("Received message from redis channel {channel}", channelName);

                var message = DeserializeMessage<HubMessage>(data);

                // TODO: This isn't going to work when we allow JsonSerializer customization or add Protobuf
                var tasks = new List<Task>(_connections.Count);

                foreach (var connection in _connections)
                {
                    tasks.Add(WriteAsync(connection, message));
                }

                previousBroadcastTask = Task.WhenAll(tasks);
            });

            var allExceptTask = Task.CompletedTask;
            channelName = _channelNamePrefix + ".AllExcept";
            _logger.LogInformation("Subscribing to channel: {channel}", channelName);
            _bus.Subscribe(channelName, async (c, data) =>
            {
                await allExceptTask;

                _logger.LogTrace("Received message from redis channel {channel}", channelName);

                var message = DeserializeMessage<RedisExcludeClientsMessage>(data);
                var excludedIds = message.ExcludedIds;

                // TODO: This isn't going to work when we allow JsonSerializer customization or add Protobuf

                var tasks = new List<Task>(_connections.Count);

                foreach (var connection in _connections)
                {
                    if (!excludedIds.Contains(connection.ConnectionId))
                    {
                        tasks.Add(WriteAsync(connection, message));
                    }
                }

                allExceptTask = Task.WhenAll(tasks);
            });
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix, message);
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            var message = new RedisExcludeClientsMessage(GetInvocationId(), nonBlocking: true, target: methodName, excludedIds: excludedIds, arguments: args);
            return PublishAsync(_channelNamePrefix + ".AllExcept", message);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + "." + connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + ".group." + groupName, message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + ".user." + userId, message);
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

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            var feature = new RedisFeature();
            connection.Features.Set<IRedisFeature>(feature);

            var redisSubscriptions = feature.Subscriptions;
            var connectionTask = Task.CompletedTask;
            var userTask = Task.CompletedTask;

            _connections.Add(connection);

            var connectionChannel = _channelNamePrefix + "." + connection.ConnectionId;
            redisSubscriptions.Add(connectionChannel);

            var previousConnectionTask = Task.CompletedTask;

            _logger.LogInformation("Subscribing to connection channel: {channel}", connectionChannel);
            connectionTask = _bus.SubscribeAsync(connectionChannel, async (c, data) =>
            {
                await previousConnectionTask;

                var message = DeserializeMessage<HubMessage>(data);

                previousConnectionTask = WriteAsync(connection, message);
            });

            if (connection.User.Identity.IsAuthenticated)
            {
                var userChannel = _channelNamePrefix + ".user." + connection.User.Identity.Name;
                redisSubscriptions.Add(userChannel);

                var previousUserTask = Task.CompletedTask;

                // TODO: Look at optimizing (looping over connections checking for Name)
                userTask = _bus.SubscribeAsync(userChannel, async (c, data) =>
                {
                    await previousUserTask;

                    var message = DeserializeMessage<HubMessage>(data);

                    previousUserTask = WriteAsync(connection, message);
                });
            }

            return Task.WhenAll(connectionTask, userTask);
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);

            var tasks = new List<Task>();

            var feature = connection.Features.Get<IRedisFeature>();

            var redisSubscriptions = feature.Subscriptions;
            if (redisSubscriptions != null)
            {
                foreach (var subscription in redisSubscriptions)
                {
                    _logger.LogInformation("Unsubscribing from channel: {channel}", subscription);
                    tasks.Add(_bus.UnsubscribeAsync(subscription));
                }
            }

            var groupNames = feature.Groups;

            if (groupNames != null)
            {
                // Copy the groups to an array here because they get removed from this collection
                // in RemoveGroupAsync
                foreach (var group in groupNames.ToArray())
                {
                    tasks.Add(RemoveGroupAsync(connection.ConnectionId, group));
                }
            }

            return Task.WhenAll(tasks);
        }

        public override async Task AddGroupAsync(string connectionId, string groupName)
        {
            var groupChannel = _channelNamePrefix + ".group." + groupName;
            var connection = _connections[connectionId];
            if (connection == null)
            {
                return;
            }

            var feature = connection.Features.Get<IRedisFeature>();
            var groupNames = feature.Groups;

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

                    var message = DeserializeMessage<HubMessage>(data);

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

        public override async Task RemoveGroupAsync(string connectionId, string groupName)
        {
            var groupChannel = _channelNamePrefix + ".group." + groupName;

            GroupData group;
            if (!_groups.TryGetValue(groupChannel, out group))
            {
                return;
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return;
            }

            var feature = connection.Features.Get<IRedisFeature>();
            var groupNames = feature.Groups;
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

        private async Task WriteAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            while (await connection.Output.WaitToWriteAsync())
            {
                if (connection.Output.TryWrite(hubMessage))
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

        private T DeserializeMessage<T>(RedisValue data)
        {
            using (var reader = new JsonTextReader(new StreamReader(new MemoryStream(data))))
            {
                return (T)_serializer.Deserialize(reader);
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

        public class RedisExcludeClientsMessage : InvocationMessage
        {
            public IReadOnlyList<string> ExcludedIds;

            public RedisExcludeClientsMessage(string invocationId, bool nonBlocking, string target, IReadOnlyList<string> excludedIds, params object[] arguments)
                : base(invocationId, nonBlocking, target, arguments)
            {
                ExcludedIds = excludedIds;
            }
        }

        private class GroupData
        {
            public SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
            public HubConnectionList Connections = new HubConnectionList();
        }

        private interface IRedisFeature
        {
            HashSet<string> Subscriptions { get; }
            HashSet<string> Groups { get; }
        }

        private class RedisFeature : IRedisFeature
        {
            public HashSet<string> Subscriptions { get; } = new HashSet<string>();
            public HashSet<string> Groups { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
