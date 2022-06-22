// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents vary-by rules.
/// </summary>
public sealed class CacheVaryByRules
{
    private Dictionary<string, string>? _varyByCustom;

    internal bool HasVaryByCustom => _varyByCustom != null && _varyByCustom.Any();

    /// <summary>
    /// Gets a dictionary of key-pair values to vary the cache by.
    /// </summary>
    public IDictionary<string, string> VaryByCustom => _varyByCustom ??= new();

    /// <summary>
    /// Gets or sets the list of headers to vary by.
    /// </summary>
    public StringValues Headers { get; set; }

    /// <summary>
    /// Gets or sets the list of query string keys to vary by.
    /// </summary>
    public StringValues QueryKeys { get; set; }

    /// <summary>
    /// Gets or sets a prefix to vary by.
    /// </summary>
    public StringValues VaryByPrefix { get; set; }
}
