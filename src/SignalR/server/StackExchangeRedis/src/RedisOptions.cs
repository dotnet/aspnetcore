// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using StackExchange.Redis;
using StackExchange.Redis.Configuration;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis;

/// <summary>
/// Options used to configure <see cref="RedisHubLifetimeManager{THub}"/>.
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Gets or sets configuration options exposed by <c>StackExchange.Redis</c>.
    /// </summary>
    public ConfigurationOptions Configuration { get; set; } = new ConfigurationOptions
    {
        // Enable reconnecting by default
        AbortOnConnectFail = false
    };

    /// <summary>
    /// Gets or sets the Redis connection factory.
    /// </summary>
    public Func<TextWriter, Task<IConnectionMultiplexer>>? ConnectionFactory { get; set; }

    internal async Task<IConnectionMultiplexer> ConnectAsync(TextWriter log)
    {
        // Factory is publicly settable. Assigning to a local variable before null check for thread safety.
        var factory = ConnectionFactory;
        if (factory == null)
        {
            // REVIEW: Should we do this?
            if (Configuration.EndPoints.Count == 0)
            {
                Configuration.EndPoints.Add(IPAddress.Loopback, 0);
                Configuration.SetDefaultPorts();
            }

            // suffix SignalR onto the declared library name
            var provider = DefaultOptionsProvider.GetProvider(Configuration.EndPoints);
            Configuration.LibraryName = $"{provider.LibraryName} SignalR";

            return await ConnectionMultiplexer.ConnectAsync(Configuration, log);
        }

        return await factory(log);
    }
}
