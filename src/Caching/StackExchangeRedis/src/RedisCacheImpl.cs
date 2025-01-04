// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal sealed class RedisCacheImpl : RedisCache
{
    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, ILogger<RedisCache> logger, IServiceProvider services)
        : base(optionsAccessor, logger)
    {
        HybridCacheActive = IsHybridCacheDefined(services);
    }

    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, IServiceProvider services)
        : base(optionsAccessor)
    {
        HybridCacheActive = IsHybridCacheDefined(services);
    }

    // HybridCache optionally uses IDistributedCache; if we're here, then *we are* the DC
    private static bool IsHybridCacheDefined(IServiceProvider services)
        => services.GetService<HybridCache>() is not null;
}
