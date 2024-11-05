// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.Configuration;
using StackExchange.Redis.Profiling;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;

/// <summary>
/// Configuration options for Redis based output cache.
/// </summary>
public sealed class RedisOutputCacheOptions
{
    /// <summary>
    /// The configuration used to connect to Redis.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// The configuration used to connect to Redis.
    /// This is preferred over Configuration.
    /// </summary>
    public ConfigurationOptions? ConfigurationOptions { get; set; }

    /// <summary>
    /// Gets or sets a delegate to create the ConnectionMultiplexer instance.
    /// </summary>
    public Func<Task<IConnectionMultiplexer>>? ConnectionMultiplexerFactory { get; set; }

    /// <summary>
    /// The Redis instance name. Allows partitioning a single backend cache for use with multiple apps/services.
    /// If set, the cache keys are prefixed with this value.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// The Redis profiling session
    /// </summary>
    public Func<ProfilingSession>? ProfilingSession { get; set; }

    private bool? _useForceReconnect;
    internal bool UseForceReconnect
    {
        get
        {
            return _useForceReconnect ??= GetDefaultValue();
            static bool GetDefaultValue() =>
                AppContext.TryGetSwitch("Microsoft.AspNetCore.Caching.StackExchangeRedis.UseForceReconnect", out var value) && value;
        }
        set => _useForceReconnect = value;
    }

    internal ConfigurationOptions GetConfiguredOptions()
    {
        var options = ConfigurationOptions ?? ConfigurationOptions.Parse(Configuration!);

        // we don't want an initially unavailable server to prevent DI creating the service itself
        options.AbortOnConnectFail = false;

        return options;
    }
}
