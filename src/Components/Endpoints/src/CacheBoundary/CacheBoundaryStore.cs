// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal interface ICacheBoundaryStore : IDisposable
{
    ValueTask<byte[]> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<byte[]>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken);

    void Clear() { }
}
