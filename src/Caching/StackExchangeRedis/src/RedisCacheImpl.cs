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
    private readonly IServiceProviderIsService? _serviceProviderIsService;

    internal override bool IsHybridCacheActive()
        => _serviceProviderIsService?.IsService(typeof(HybridCache)) == true;

    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, ILogger<RedisCache> logger, IServiceProviderIsService? serviceProviderIsService = null)
        : base(optionsAccessor, logger)
    {
        _serviceProviderIsService = serviceProviderIsService;
    }

    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, IServiceProviderIsService? serviceProviderIsService = null)
        : base(optionsAccessor)
    {
        _serviceProviderIsService = serviceProviderIsService;
    }
}
