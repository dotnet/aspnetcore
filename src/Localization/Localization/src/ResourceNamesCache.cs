// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// An implementation of <see cref="IResourceNamesCache"/> backed by a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
public class ResourceNamesCache : IResourceNamesCache
{
    private readonly ConcurrentDictionary<string, IList<string>?> _cache = new ConcurrentDictionary<string, IList<string>?>();

    /// <summary>
    /// Creates a new <see cref="ResourceNamesCache" />
    /// </summary>
    public ResourceNamesCache()
    {
    }

    /// <inheritdoc />
    public IList<string>? GetOrAdd(string name, Func<string, IList<string>?> valueFactory)
    {
        return _cache.GetOrAdd(name, valueFactory);
    }
}
