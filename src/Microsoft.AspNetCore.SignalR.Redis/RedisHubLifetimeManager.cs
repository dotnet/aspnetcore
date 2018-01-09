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
        private readonly IConnectionMultiplexer _redisServerConnection;
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
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None,
        };

        public RedisHubLifetimeManager(ILogger<RedisHubLifetimeManager<THub>> logger,
                                       IOptions<RedisOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _ackHandler = new AckHandler();

            var writer = new LoggerTextWriter(logger);
            _logger.ConnectingToEndpoints(options.Value.Options.EndPoints);
            _redisServerConnection = _options.Connect(writer);

            _redisServerConnection.ConnectionRestored += (_, e) =>
            {
                // We use the subscription connection type
                // Ignore messages from the interactive connection (avoids duplicates)
                if (e.ConnectionType == ConnectionType.Interactive)
                {
                    return;
                }

                _logger.ConnectionRestored();
            };

            _redisServerConnection.ConnectionFailed += (_, e) =>
            {
                // We use the subscription connection type
                // Ignore messages from the interactive connection (avoids duplicates)
                if (e.ConnectionType == ConnectionType.Interactive)
                {
                    return;
                }

                _logger.ConnectionFailed(e.Exception);
            };

            if (_redisServerConnection.IsConnected)
            {
                _logger.Connected();
            }
            else
            {
                _logger.NotConnected();
            }
            _bus = _redisServerConnection.GetSubscriber();

            SubscribeToHub();
            SubscribeToAllExcept();
            SubscribeToInternalGroup();
            SubscribeToInternalServerName();
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            var feature = new RedisFeature();
            connection.Features.Set<IRedisFeature>(feature);

            var redisSubscriptions = feature.Subscriptions;
            var connectionTask = Task.CompletedTask;
            var userTask = Task.CompletedTask;

            _connections.Add(connection);

            connectionTask = SubscribeToConnection(connection, redisSubscriptions);

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
            {
                userTask = SubscribeToUser(connection, redisSubscriptions);
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
                    _logger.Unsubscribe(subscription);
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
                    // Use RemoveGroupAsyncCore because the connection is local and we don't want to
                    // accidentally go to other servers with our remove request.
                    tasks.Add(RemoveGroupAsyncCore(connection, group));
                }
            }

            return Task.WhenAll(tasks);
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            var message = new RedisInvocationMessage(target: methodName, arguments: args);

            return PublishAsync(_channelNamePrefix, message);
        }

        public override Task InvokeAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            var message = new RedisInvocationMessage(target: methodName, excludedIds: excludedIds, arguments: args);
            return PublishAsync(_channelNamePrefix + ".AllExcept", message);
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            var message = new RedisInvocationMessage(target: methodName, arguments: args);

            // If the connection is local we can skip sending the message through the bus since we require sticky connections.
            // This also saves serializing and deserializing the message!
            var connection = _connections[connectionId];
            if (connection != null)
            {
                return connection.WriteAsync(message.CreateInvocation());
            }

            return PublishAsync(_channelNamePrefix + "." + connectionId, message);
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var message = new RedisInvocationMessage(target: methodName, excludedIds: null, arguments: args);

            return PublishAsync(_channelNamePrefix + ".group." + groupName, message);
        }

        public override Task InvokeGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedIds)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var message = new RedisInvocationMessage(methodName, excludedIds, args);

            return PublishAsync(_channelNamePrefix + ".group." + groupName, message);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            var message = new RedisInvocationMessage(methodName, args);

            return PublishAsync(_channelNamePrefix + ".user." + userId, message);
        }

        private async Task PublishAsync(string channel, IRedisMessage message)
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new JsonTextWriter(new StreamWriter(stream)))
            {
                _serializer.Serialize(writer, message);
                await writer.FlushAsync();
                payload = stream.ToArray();
            }

            _logger.PublishToChannel(channel);
            await _bus.PublishAsync(channel, payload);
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

            var connection = _connections[connectionId];
            if (connection != null)
            {
                // short circuit if connection is on this server
                await AddGroupAsyncCore(connection, groupName);
                return;
            }

            await SendGroupActionAndWaitForAck(connectionId, groupName, GroupAction.Add);
        }

        private async Task AddGroupAsyncCore(HubConnectionContext connection, string groupName)
        {
            var feature = connection.Features.Get<IRedisFeature>();
            var groupNames = feature.Groups;

            lock (groupNames)
            {
                // Connection already in group
                if (!groupNames.Add(groupName))
                {
                    return;
                }
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
                    return;
                }

                await SubscribeToGroup(groupChannel, group);
            }
            finally
            {
                group.Lock.Release();
            }
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


            var connection = _connections[connectionId];
            if (connection != null)
            {
                // short circuit if connection is on this server
                await RemoveGroupAsyncCore(connection, groupName);
                return;
            }

            await SendGroupActionAndWaitForAck(connectionId, groupName, GroupAction.Remove);
        }

        /// <summary>
        /// This takes <see cref="HubConnectionContext"/> because we want to remove the connection from the
        /// _connections list in OnDisconnectedAsync and still be able to remove groups with this method.
        /// </summary>
        private async Task RemoveGroupAsyncCore(HubConnectionContext connection, string groupName)
        {
            var groupChannel = _channelNamePrefix + ".group." + groupName;

            GroupData group;
            if (!_groups.TryGetValue(groupChannel, out group))
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
                if (group.Connections.Count > 0)
                {
                    group.Connections.Remove(connection);

                    if (group.Connections.Count == 0)
                    {
                        _logger.Unsubscribe(groupChannel);
                        await _bus.UnsubscribeAsync(groupChannel);
                    }
                }
            }
            finally
            {
                group.Lock.Release();
            }

            return;
        }

        private async Task SendGroupActionAndWaitForAck(string connectionId, string groupName, GroupAction action)
        {
            var id = Interlocked.Increment(ref _internalId);
            var ack = _ackHandler.CreateAck(id);
            // Send Add/Remove Group to other servers and wait for an ack or timeout
            await PublishAsync(_channelNamePrefix + ".internal.group", new RedisGroupMessage
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

        private T DeserializeMessage<T>(RedisValue data)
        {
            using (var reader = new JsonTextReader(new StreamReader(new MemoryStream(data))))
            {
                return _serializer.Deserialize<T>(reader);
            }
        }

        private void SubscribeToHub()
        {
            _logger.Subscribing(_channelNamePrefix);
            _bus.Subscribe(_channelNamePrefix, async (c, data) =>
            {
                try
                {
                    _logger.ReceivedFromChannel(_channelNamePrefix);

                    var message = DeserializeMessage<RedisInvocationMessage>(data);

                    var tasks = new List<Task>(_connections.Count);

                    var invocation = message.CreateInvocation();
                    foreach (var connection in _connections)
                    {
                        tasks.Add(connection.WriteAsync(invocation));
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.FailedWritingMessage(ex);
                }
            });
        }

        private void SubscribeToAllExcept()
        {
            var channelName = _channelNamePrefix + ".AllExcept";
            _logger.Subscribing(channelName);
            _bus.Subscribe(channelName, async (c, data) =>
            {
                try
                {
                    _logger.ReceivedFromChannel(channelName);

                    var message = DeserializeMessage<RedisInvocationMessage>(data);
                    var excludedIds = message.ExcludedIds ?? Array.Empty<string>();

                    var tasks = new List<Task>(_connections.Count);

                    var invocation = message.CreateInvocation();
                    foreach (var connection in _connections)
                    {
                        if (!excludedIds.Contains(connection.ConnectionId))
                        {
                            tasks.Add(connection.WriteAsync(invocation));
                        }
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.FailedWritingMessage(ex);
                }
            });
        }

        private void SubscribeToInternalGroup()
        {
            var channelName = _channelNamePrefix + ".internal.group";
            _bus.Subscribe(channelName, async (c, data) =>
            {
                try
                {
                    var groupMessage = DeserializeMessage<RedisGroupMessage>(data);

                    var connection = _connections[groupMessage.ConnectionId];
                    if (connection == null)
                    {
                        // user not on this server
                        return;
                    }

                    if (groupMessage.Action == GroupAction.Remove)
                    {
                        await RemoveGroupAsyncCore(connection, groupMessage.Group);
                    }

                    if (groupMessage.Action == GroupAction.Add)
                    {
                        await AddGroupAsyncCore(connection, groupMessage.Group);
                    }

                    // Sending ack to server that sent the original add/remove
                    await PublishAsync($"{_channelNamePrefix}.internal.{groupMessage.Server}", new RedisGroupMessage
                    {
                        Action = GroupAction.Ack,
                        Id = groupMessage.Id
                    });
                }
                catch (Exception ex)
                {
                    _logger.InternalMessageFailed(ex);
                }
            });
        }

        private void SubscribeToInternalServerName()
        {
            // Create server specific channel in order to send an ack to a single server
            var serverChannel = $"{_channelNamePrefix}.internal.{_serverName}";
            _bus.Subscribe(serverChannel, (c, data) =>
            {
                var groupMessage = DeserializeMessage<RedisGroupMessage>(data);

                if (groupMessage.Action == GroupAction.Ack)
                {
                    _ackHandler.TriggerAck(groupMessage.Id);
                }
            });
        }

        private Task SubscribeToConnection(HubConnectionContext connection, HashSet<string> redisSubscriptions)
        {
            var connectionChannel = _channelNamePrefix + "." + connection.ConnectionId;
            redisSubscriptions.Add(connectionChannel);

            _logger.Subscribing(connectionChannel);
            return _bus.SubscribeAsync(connectionChannel, async (c, data) =>
            {
                try
                {
                    var message = DeserializeMessage<RedisInvocationMessage>(data);

                    await connection.WriteAsync(message.CreateInvocation());
                }
                catch (Exception ex)
                {
                    _logger.FailedWritingMessage(ex);
                }
            });
        }

        private Task SubscribeToUser(HubConnectionContext connection, HashSet<string> redisSubscriptions)
        {
            var userChannel = _channelNamePrefix + ".user." + connection.UserIdentifier;
            redisSubscriptions.Add(userChannel);

            // TODO: Look at optimizing (looping over connections checking for Name)
            return _bus.SubscribeAsync(userChannel, async (c, data) =>
            {
                try
                {
                    var message = DeserializeMessage<HubInvocationMessage>(data);

                    await connection.WriteAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.FailedWritingMessage(ex);
                }
            });
        }

        private Task SubscribeToGroup(string groupChannel, GroupData group)
        {
            _logger.Subscribing(groupChannel);
            return _bus.SubscribeAsync(groupChannel, async (c, data) =>
            {
                try
                {
                    var message = DeserializeMessage<RedisInvocationMessage>(data);

                    var tasks = new List<Task>();
                    var invocation = message.CreateInvocation();
                    foreach (var groupConnection in group.Connections)
                    {
                        if (message.ExcludedIds?.Contains(groupConnection.ConnectionId) == true)
                        {
                            continue;
                        }

                        tasks.Add(groupConnection.WriteAsync(invocation));
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.FailedWritingMessage(ex);
                }
            });
        }

        public override Task InvokeConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args)
        {
            if (connectionIds == null)
            {
                throw new ArgumentNullException(nameof(connectionIds));
            }
            var publishTasks = new List<Task>(connectionIds.Count);
            var message = new RedisInvocationMessage(target: methodName, arguments: args);
            
            foreach(string connectionId in connectionIds)
            {
                var connection = _connections[connectionId];
                // If the connection is local we can skip sending the message through the bus since we require sticky connections.
                // This also saves serializing and deserializing the message!
                if (connection != null)
                {
                     publishTasks.Add(connection.WriteAsync(message.CreateInvocation()));
                }
                else
                {
                    publishTasks.Add(PublishAsync(_channelNamePrefix + "." + connectionId, message));
                }
            }

            return Task.WhenAll(publishTasks);
        }

        public override Task InvokeGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args)
        {
            if (groupNames == null)
            {
                throw new ArgumentNullException(nameof(groupNames));
            }
            var publishTasks = new List<Task>(groupNames.Count);
            var message = new RedisInvocationMessage(target: methodName, arguments: args);

            foreach (var groupName in groupNames)
            {
                if (!string.IsNullOrEmpty(groupName))
                {
                    publishTasks.Add(PublishAsync(_channelNamePrefix + "." + groupName, message));
                }
            }

            return Task.WhenAll(publishTasks);
        }

        public override Task InvokeUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args)
        {
            if (userIds.Count > 0)
            {
                var message = new RedisInvocationMessage(methodName, args);
                var publishTasks = new List<Task>(userIds.Count);
                foreach (var userId in userIds)
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        publishTasks.Add(PublishAsync(_channelNamePrefix + ".user." + userId, message));
                    }
                }

                return Task.WhenAll(publishTasks);
            }

            return Task.CompletedTask;
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

        // Marker interface to represent the messages that can be sent over Redis.
        private interface IRedisMessage { }

        private class RedisGroupMessage : IRedisMessage
        {
            public string ConnectionId { get; set; }
            public string Group { get; set; }
            public int Id { get; set; }
            public GroupAction Action { get; set; }
            public string Server { get; set; }
        }

        // Represents a message published to the Redis bus
        private class RedisInvocationMessage : IRedisMessage
        {
            public string Target { get; set; }
            public IReadOnlyList<string> ExcludedIds { get; set; }
            public object[] Arguments { get; set; }

            public RedisInvocationMessage()
            {
            }

            public RedisInvocationMessage(string target, object[] arguments)
                : this(target, excludedIds: null, arguments: arguments)
            {
            }

            public RedisInvocationMessage(string target, IReadOnlyList<string> excludedIds, object[] arguments)
            {
                Target = target;
                ExcludedIds = excludedIds;
                Arguments = arguments;
            }

            public InvocationMessage CreateInvocation()
            {
                return new InvocationMessage(Target, argumentBindingException: null, arguments: Arguments);
            }
        }
    }
}
