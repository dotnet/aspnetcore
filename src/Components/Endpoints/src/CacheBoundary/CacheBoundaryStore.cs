// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides a store for caching rendered component output as a JSON template-with-holes representation.
/// </summary>
internal interface ICacheBoundaryStore : IDisposable
{
    /// <summary>
    /// Gets a cached JSON template for the specified key, or <c>null</c> on cache miss.
    /// </summary>
    string? Get(string key);

    /// <summary>
    /// Stores a JSON template for the specified key.
    /// </summary>
    void Set(string key, string json, CacheStoreOptions options = default);

    /// <summary>
    /// Removes all cached entries. Used primarily for testing scenarios.
    /// </summary>
    void Clear() { }
}
