// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER // IOutputCacheStore only exists from net7

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal sealed class RedisOutputCacheStoreImpl : RedisOutputCacheStore
{
    public RedisOutputCacheStoreImpl(IOptions<RedisCacheOptions> optionsAccessor, ILogger<RedisCache> logger)
        : base(optionsAccessor, logger)
    {
    }

    public RedisOutputCacheStoreImpl(IOptions<RedisCacheOptions> optionsAccessor)
        : base(optionsAccessor)
    {
    }
}

#endif
