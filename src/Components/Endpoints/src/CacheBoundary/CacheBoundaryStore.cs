// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides a store for caching rendered component output as a JSON template-with-holes representation.
/// </summary>
internal interface ICacheBoundaryStore : IDisposable
{
    /// <summary>
    /// Gets the cached JSON template for <paramref name="key"/>, or invokes <paramref name="factory"/>
    /// to produce it on cache miss. Concurrent callers for the same key share a single factory
    /// invocation; waiters observe the same returned value.
    /// </summary>
    ValueTask<string> GetOrCreateAsync(
        string key,
        Func<CancellationToken, ValueTask<string>> factory,
        CacheStoreOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes all cached entries. Used primarily for testing scenarios.
    /// </summary>
    void Clear() { }
}
