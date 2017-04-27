// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Redis;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ChatSample
{
    public class RedisUserTracker<THub> : IUserTracker<THub>
    {
        private readonly RedisKey UsersOnlineRedisKey = "UsersOnline";
        private readonly int _redisDatabase;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly ISubscriber _redisSubscriber;
        private readonly ILogger _logger;
        private readonly RedisChannel _redisChannel;

        public event Action<UserDetails> UserJoined;
        public event Action<UserDetails> UserLeft;

        public RedisUserTracker(IOptions<RedisOptions> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RedisUserTracker<THub>>();
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
                    UserLeft(user);
                }
                else
                {
                    UserJoined(user);
                }
            });
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

        private static UserDetails ToUserDetails(string user)
        {
            var pos = user.IndexOf("|");
            Debug.Assert(pos >= 0, "Invalid user details format");
            return new UserDetails(user.Substring(0, pos), user.Substring(pos + 1));
        }

        public async Task AddUser(Connection connection, UserDetails userDetails)
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            var user = $"{connection.ConnectionId}|{connection.User.Identity.Name}";

            // need to await to make sure user is added before we call into the Hub
            await database.SetAddAsync(UsersOnlineRedisKey, $"{connection.ConnectionId}|{connection.User.Identity.Name}");
            _ = _redisSubscriber.PublishAsync(_redisChannel, "+" + user);
        }

        public async Task RemoveUser(Connection connection)
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            var user = $"{connection.ConnectionId}|{connection.User.Identity.Name}";

            await database.SetRemoveAsync(UsersOnlineRedisKey, user);
            _ = _redisSubscriber.PublishAsync(_redisChannel, "-" + user);
        }

        public async Task<IEnumerable<UserDetails>> UsersOnline()
        {
            var database = _redisConnection.GetDatabase(_redisDatabase);
            var usersOnline = await database.SetMembersAsync(UsersOnlineRedisKey);
            return usersOnline.Select(u => ToUserDetails(u));
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