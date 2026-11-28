// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace ResponseCachingCompatibility;

/// <summary>
/// Counts how many times each endpoint ran in an application, to show whether a response came from the cache.
/// </summary>
public sealed class EndpointCounter
{
    private readonly ConcurrentDictionary<string, int> _counts = new();

    public int Next(string endpoint) => _counts.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
}
