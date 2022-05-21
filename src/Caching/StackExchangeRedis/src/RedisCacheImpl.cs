// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal sealed class RedisCacheImpl : RedisCache
{
    public RedisCacheImpl(IOptions<RedisCacheOptions> optionsAccessor, ILogger<RedisCacheImpl> logger)
        : base(optionsAccessor, logger)
    {
    }
}
