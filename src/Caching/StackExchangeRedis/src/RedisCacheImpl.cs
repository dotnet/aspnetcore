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
    private readonly IServiceProvider _services;

    internal override bool IsHybridCacheActive()
        => _services.GetService<HybridCache>() is not null;

    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, ILogger<RedisCache> logger, IServiceProvider services)
        : base(optionsAccessor, logger)
    {
        _services = services; // important: do not check for HybridCache here due to dependency - creates a cycle
    }

    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, IServiceProvider services)
        : base(optionsAccessor)
    {
        _services = services; // important: do not check for HybridCache here due to dependency - creates a cycle
    }
}
