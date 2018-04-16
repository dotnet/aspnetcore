// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Redis;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisDependencyInjectionExtensions
    {
        public static ISignalRServerBuilder AddRedis(this ISignalRServerBuilder builder)
        {
            return AddRedis(builder, o => { });
        }

        public static ISignalRServerBuilder AddRedis(this ISignalRServerBuilder builder, string redisConnectionString)
        {
            return AddRedis(builder, o =>
            {
                o.Configuration = ConfigurationOptions.Parse(redisConnectionString);
            });
        }

        public static ISignalRServerBuilder AddRedis(this ISignalRServerBuilder builder, Action<RedisOptions> configure)
        {
            builder.Services.Configure(configure);
            builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(RedisHubLifetimeManager<>));
            return builder;
        }

        public static ISignalRServerBuilder AddRedis(this ISignalRServerBuilder builder, string redisConnectionString, Action<RedisOptions> configure)
        {
            return AddRedis(builder, o =>
            {
                o.Configuration = ConfigurationOptions.Parse(redisConnectionString);
                configure(o);
            });
        }
    }
}
