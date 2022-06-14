// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents vary-by rules.
/// </summary>
public class CachedVaryByRules
{
    internal Dictionary<string, string>? VaryByCustom;

    /// <summary>
    /// Defines a custom key-pair value to vary the cache by.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void SetVaryByCustom(string key, string value)
    {
        VaryByCustom ??= new();
        VaryByCustom[key] = value;
    }

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
