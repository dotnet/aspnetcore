// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ChatSample
{
    public class RedisUserTracker<THub> : IUserTracker<THub>, IDisposable
    {
        private readonly string ServerId = $"server:{Guid.NewGuid().ToString("D")}";
        private readonly RedisKey ServerIndexRedisKey = "ServerIndex";
        private readonly RedisKey UserIndexRedisKey;
        private readonly int _redisDatabase;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly ISubscriber _redisSubscriber;
        private readonly ILogger _logger;

        private const string UserAddedChannelName = "UserAdded";
        private const string UserRemovedChannelName = "UserRemoved";
        private readonly RedisChannel _userAddedChannel;
        private readonly RedisChannel _userRemovedChannel;

        public event Action<UserDetails> UserJoined;
        public event Action<UserDetails> UserLeft;

        public RedisUserTracker(IOptions<RedisOptions> options, ILoggerFactory loggerFactory)
        {
            UserIndexRedisKey = $"{ServerId}:users";

            _logger = loggerFactory.CreateLogger<RedisUserTracker<THub>>();
            _redisDatabase = options.Value.Options.DefaultDatabase.GetValueOrDefault();
            _redisConnection = StartRedisConnection(options.Value);

            _redisSubscriber = _redisConnection.GetSubscriber();
            _userAddedChannel = new RedisChannel(UserAddedChannelName, RedisChannel.PatternMode.Literal);
            _userRemovedChannel = new RedisChannel(UserRemovedChannelName, RedisChannel.PatternMode.Literal);
            _redisSubscriber.Subscribe(_userAddedChannel, (channel, value) => UserJoined(DeserializerUser(value)));
            _redisSubscriber.Subscribe(_userRemovedChannel, (channel, value) => UserLeft(DeserializerUser(value)));
        }

        private ConnectionMultiplexer StartRedisConnection(RedisOptions options)
        {
            // TODO: handle connection failures
            var redisConnection = ConnectToRedis(options, _logger);

            // Register connection
            var database = redisConnection.GetDatabase(_redisDatabase);
            database.SetAdd(ServerIndexRedisKey, ServerId);

            return redisConnection;
        }

        private static ConnectionMultiplexer ConnectToRedis(RedisOptions options, ILogger logger)
        {
            var loggerTextWriter = new LoggerTextWriter(logger);
            if (options.Factory != null)
            {
                return options.Factory(loggerTextWriter);
            }

            if (options.Options.EndPoints.Any())
            {
                return ConnectionMultiplexer.Connect(options.Options, loggerTextWriter);
            }

            var configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(IPAddress.Loopback, 0);
            configurationOptions.SetDefaultPorts();

            return ConnectionMultiplexer.Connect(configurationOptions, loggerTextWriter);
        }

        public async Task AddUser(Connection connection, UserDetails userDetails)
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            var key = GetUserRedisKey(connection);
            var user = SerializeUser(connection);
            // need to await to make sure user is added before we call into the Hub
            await database.StringSetAsync(key, SerializeUser(connection));
            await database.SetAddAsync(UserIndexRedisKey, key);
            _ = _redisSubscriber.PublishAsync(_userAddedChannel, user);
        }

        public async Task RemoveUser(Connection connection)
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            await database.SetRemoveAsync(UserIndexRedisKey, connection.ConnectionId);
            if (await database.KeyDeleteAsync(GetUserRedisKey(connection)))
            {
                _ = _redisSubscriber.PublishAsync(_userRemovedChannel, SerializeUser(connection));
            }
        }

        public async Task<IEnumerable<UserDetails>> UsersOnline()
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);

            var userIds = await database.ScriptEvaluateAsync(
                @"local keys = { }
                for i, key in pairs(redis.call('smembers', KEYS[1])) do
                        table.insert(keys, key.. ':users')
                end
                return redis.call('sunion', unpack(keys))", new[] { ServerIndexRedisKey });

            if (!userIds.IsNull)
            {
                var users = await database.StringGetAsync(((RedisValue[])userIds).Select(id => (RedisKey)(string)id).ToArray());
                return users.Where(user => !user.IsNull).Select(user => DeserializerUser(user));
            }

            return Enumerable.Empty<UserDetails>();
        }

        private static string GetUserRedisKey(Connection connection) => $"user:{connection.ConnectionId}";

        private static string SerializeUser(Connection connection) =>
            $"{{ \"ConnectionID\": \"{connection.ConnectionId}\", \"Name\": \"{connection.User.Identity.Name}\" }}";

        private static UserDetails DeserializerUser(string userJson) =>
            JsonConvert.DeserializeObject<UserDetails>(userJson);

        public void Dispose()
        {
            _redisSubscriber.UnsubscribeAll();
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