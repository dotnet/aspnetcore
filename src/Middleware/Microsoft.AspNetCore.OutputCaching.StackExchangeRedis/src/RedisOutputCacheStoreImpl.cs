// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;

internal sealed class RedisOutputCacheStoreImpl : RedisOutputCacheStore
{
    public RedisOutputCacheStoreImpl(IOptions<RedisOutputCacheOptions> optionsAccessor, ILogger<RedisOutputCacheStore> logger)
        : base(optionsAccessor, logger)
    {
    }

    public RedisOutputCacheStoreImpl(IOptions<RedisOutputCacheOptions> optionsAccessor)
        : base(optionsAccessor)
    {
    }
}
