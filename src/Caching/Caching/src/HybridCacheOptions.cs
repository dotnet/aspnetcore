// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Distributed;
public class HybridCacheOptions
{

    public HybridCacheEntryOptions? DefaultOptions { get; set; }

    public bool AllowCompression { get; set; } = true;
    public long MaximumPayloadBytes { get; set; } = 1 << 20; // 1MiB
    public int MaximumKeyLength { get; set; } = 1024; // characters

    // opt-in support for using "tags" as dimensions on metric reporting; this is opt-in
    // because "tags" could contain user data, which we do not want to expose by default
    public bool ReportTagMetrics { get; set; }
}
