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
using Microsoft.AspNetCore.SignalR.Redis.Internal;
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
        private readonly string _serverName = Guid.NewGuid().ToString();
        private readonly AckHandler _ackHandler;
        private int _internalId;

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
            _ackHandler = new AckHandler();

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

            channelName = _channelNamePrefix + ".internal.group";
            _bus.Subscribe(channelName, async (c, data) =>
            {
                var groupMessage = DeserializeMessage<GroupMessage>(data);

                if (groupMessage.Action == GroupAction.Remove)
                {
                    if (!await RemoveGroupAsyncCore(groupMessage.ConnectionId, groupMessage.Group))
                    {
                        // user not on this server
                        return;
                    }
                }

                if (groupMessage.Action == GroupAction.Add)
                {
                    if (!await AddGroupAsyncCore(groupMessage.ConnectionId, groupMessage.Group))
                    {
                        // user not on this server
                        return;
                    }
                }

                // Sending ack to server that sent the original add/remove
                await PublishAsync($"{_channelNamePrefix}.internal.{groupMessage.Server}", new GroupMessage
                {
                    Action = GroupAction.Ack,
                    ConnectionId = groupMessage.ConnectionId,
                    Group = groupMessage.Group,
                    Id = groupMessage.Id
                });
            });

            // Create server specific channel in order to send an ack to a single server
            var serverChannel = $"{_channelNamePrefix}.internal.{_serverName}";
            _bus.Subscribe(serverChannel, (c, data) =>
            {
                var groupMessage = DeserializeMessage<GroupMessage>(data);

                if (groupMessage.Action == GroupAction.Ack)
                {
                    _ackHandler.TriggerAck(groupMessage.Id);
                }
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
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + "." + connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + ".group." + groupName, message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix + ".user." + userId, message);
        }

        private async Task PublishAsync<TMessage>(string channel, TMessage hubMessage)
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

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
            {
                var userChannel = _channelNamePrefix + ".user." + connection.UserIdentifier;
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
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            if (await AddGroupAsyncCore(connectionId, groupName))
            {
                // short circuit if connection is on this server
                return;
            }

            await SendGroupActionAndWaitForAck(connectionId, groupName, GroupAction.Add);
        }

        private async Task<bool> AddGroupAsyncCore(string connectionId, string groupName)
        {
            var connection = _connections[connectionId];
            if (connection == null)
            {
                return false;
            }

            var feature = connection.Features.Get<IRedisFeature>();
            var groupNames = feature.Groups;

            lock (groupNames)
            {
                groupNames.Add(groupName);
            }

            var groupChannel = _channelNamePrefix + ".group." + groupName;
            var group = _groups.GetOrAdd(groupChannel, _ => new GroupData());

            await group.Lock.WaitAsync();
            try
            {
                group.Connections.Add(connection);

                // Subscribe once
                if (group.Connections.Count > 1)
                {
                    return true;
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

            return true;
        }

        public override async Task RemoveGroupAsync(string connectionId, string groupName)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            if (await RemoveGroupAsyncCore(connectionId, groupName))
            {
                // short circuit if connection is on this server
                return;
            }

            await SendGroupActionAndWaitForAck(connectionId, groupName, GroupAction.Remove);
        }

        private async Task<bool> RemoveGroupAsyncCore(string connectionId, string groupName)
        {
            var groupChannel = _channelNamePrefix + ".group." + groupName;

            GroupData group;
            if (!_groups.TryGetValue(groupChannel, out group))
            {
                return false;
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return false;
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
                if (group.Connections.Count > 0)
                {
                    group.Connections.Remove(connection);

                    if (group.Connections.Count == 0)
                    {
                        _logger.LogInformation("Unsubscribing from group channel: {channel}", groupChannel);
                        await _bus.UnsubscribeAsync(groupChannel);
                    }
                }
            }
            finally
            {
                group.Lock.Release();
            }

            return true;
        }

        private async Task SendGroupActionAndWaitForAck(string connectionId, string groupName, GroupAction action)
        {
            var id = Interlocked.Increment(ref _internalId);
            var ack = _ackHandler.CreateAck(id);
            // Send Add/Remove Group to other servers and wait for an ack or timeout
            await PublishAsync(_channelNamePrefix + ".internal.group", new GroupMessage
            {
                Action = action,
                ConnectionId = connectionId,
                Group = groupName,
                Id = id,
                Server = _serverName
            });

            await ack;
        }

        public void Dispose()
        {
            _bus.UnsubscribeAll();
            _redisServerConnection.Dispose();
            _ackHandler.Dispose();
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

        private enum GroupAction
        {
            Remove,
            Add,
            Ack
        }

        private class GroupMessage
        {
            public string ConnectionId;
            public string Group;
            public int Id;
            public GroupAction Action;
            public string Server;
        }
    }
}
