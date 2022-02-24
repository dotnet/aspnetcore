// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.SqlServer;

public class CacheItemInfo
{
    public string Id { get; set; }

    public byte[] Value { get; set; }

    public DateTimeOffset ExpiresAtTime { get; set; }

    public TimeSpan? SlidingExpirationInSeconds { get; set; }

    public DateTimeOffset? AbsoluteExpiration { get; set; }
}
