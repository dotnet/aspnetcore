// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides a store for caching rendered component output.
/// </summary>
internal interface ICacheComponentStore : IDisposable
{
    /// <summary>
    /// Attempts to retrieve a cached HTML string for the specified key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached HTML string, if found.</param>
    /// <returns><see langword="true"/> if the value was found; otherwise, <see langword="false"/>.</returns>
    bool TryGetValue(string key, out string? value);

    /// <summary>
    /// Stores a rendered HTML string in the cache with the specified key and expiration.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The HTML string to cache.</param>
    /// <param name="absoluteExpirationRelativeToNow">The absolute expiration time relative to now.</param>
    void Set(string key, string value, TimeSpan absoluteExpirationRelativeToNow);
}
