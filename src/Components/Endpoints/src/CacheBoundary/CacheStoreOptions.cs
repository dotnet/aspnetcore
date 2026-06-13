// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal readonly struct CacheStoreOptions
{
    public TimeSpan? ExpiresAfter { get; init; }
    public DateTimeOffset? ExpiresOn { get; init; }
    public TimeSpan? ExpiresSliding { get; init; }
    public CacheItemPriority? Priority { get; init; }
}
