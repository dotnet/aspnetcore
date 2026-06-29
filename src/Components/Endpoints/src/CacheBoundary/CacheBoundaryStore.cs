// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal interface ICacheBoundaryStore : IDisposable
{
    ValueTask<SerializedRenderFragment> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<SerializedRenderFragment>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken);

    void Clear() { }
}
