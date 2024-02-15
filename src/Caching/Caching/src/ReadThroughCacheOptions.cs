// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Distributed;
public class ReadThroughCacheOptions
{

    public ReadThroughCacheEntryOptions? DefaultOptions { get; set; }

    public bool AllowCompression { get; set; } = true;
    public long MaximumPayloadBytes { get; set; } = 1 << 20; // 1MiB
}
