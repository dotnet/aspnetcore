// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ChatSample
{
    public class RedisPresenceManager<THub> : IDisposable, IPresenceManager where THub : HubWithPresence
    {
        private readonly RedisKey UsersOnlineRedisKey = "UsersOnline";
        private readonly RedisChannel _redisChannel;

        private IHubContext<THub> _hubContext;
        private HubLifetimeManager<THub> _lifetimeManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly int _redisDatabase;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly ISubscriber _redisSubscriber;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<Connection, object> connections
            = new ConcurrentDictionary<Connection, object>();

        // TODO: subscribe and handle lifecycle events/disconnects
        // TODO: handle situations where Redis shuts down
        // TODO: handle situations where a server goes down and the server has zombie connections

        public RedisPresenceManager(IHubContext<THub> hubContext, HubLifetimeManager<THub> lifetimeManager,
            IServiceScopeFactory serviceScopeFactory, IOptions<RedisOptions> options, ILoggerFactory loggerFactory)
        {
            _hubContext = hubContext;
            _lifetimeManager = lifetimeManager;
            _serviceScopeFactory = serviceScopeFactory;

            _logger = loggerFactory.CreateLogger<RedisPresenceManager<THub>>();
            _redisDatabase = options.Value.Options.DefaultDatabase.GetValueOrDefault();
            _redisConnection = ConnectToRedis(options.Value, _logger);
            _redisSubscriber = _redisConnection.GetSubscriber();
            _redisChannel = new RedisChannel((string)UsersOnlineRedisKey, RedisChannel.PatternMode.Literal);

            _redisSubscriber.Subscribe(_redisChannel, (channel, value) =>
            {
                var stringValue = (string)value;
                var user = ToUserDetails(stringValue.Substring(1));
                if (stringValue[0] == '-')
                {
                    _ = Notify(hub => hub.OnUserLeft(user));
                }
                else
                {
                    _ = Notify(hub => hub.OnUserJoined(user));
                }
            });
        }

        private static ConnectionMultiplexer ConnectToRedis(RedisOptions options, ILogger logger)
        {
            var loggerTextWriter = new LoggerTextWriter(logger);
            return options.Factory == null
                ? ConnectionMultiplexer.Connect(options.Options, loggerTextWriter)
                : options.Factory(loggerTextWriter);
        }

        public async Task<IEnumerable<UserDetails>> UsersOnline()
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            var usersOnline = await database.SetMembersAsync(UsersOnlineRedisKey);

            return usersOnline.Select(u => ToUserDetails(u));
        }

        private static UserDetails ToUserDetails(string user)
        {
            var pos = user.IndexOf("|");
            Debug.Assert(pos >= 0, "Invalid user details format");
            return new UserDetails(user.Substring(0, pos), user.Substring(pos + 1));
        }

        public Task UserJoined(Connection connection)
        {
            connections.TryAdd(connection, null);

            var database = _redisConnection.GetDatabase(_redisDatabase);
            var user = $"{connection.ConnectionId}|{connection.User.Identity.Name}";
            // Fire and forget
            _ = database.SetAddAsync(UsersOnlineRedisKey, $"{connection.ConnectionId}|{connection.User.Identity.Name}");
            _ = _redisSubscriber.PublishAsync(_redisChannel, "+" + user);

            return Task.CompletedTask;
        }

        public Task UserLeft(Connection connection)
        {
            connections.TryRemove(connection, out object _);

            var database = _redisConnection.GetDatabase(_redisDatabase);
            var user = $"{connection.ConnectionId}|{connection.User.Identity.Name}";
            // Fire and forget
            _ = database.SetRemoveAsync(UsersOnlineRedisKey, user);
            _ = _redisSubscriber.PublishAsync(_redisChannel, "-" + user);

            return Task.CompletedTask;
        }

        private async Task Notify(Func<THub, Task> invocation)
        {
            foreach (var connection in connections.Keys)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, IClientProxy>>();
                    var hub = hubActivator.Create();

                    hub.Clients = _hubContext.Clients;
                    hub.Context = new HubCallerContext(connection);
                    hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);

                    try
                    {
                        await invocation(hub);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Presence notification failed.");
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
        }

        public void Dispose()
        {
            _redisSubscriber.Unsubscribe(_redisChannel);
            _redisConnection.Close();
            _redisConnection.Dispose();
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
