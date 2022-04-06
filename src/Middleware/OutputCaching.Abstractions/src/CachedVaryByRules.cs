// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents vary-by rules.
/// </summary>
public class CachedVaryByRules
{
    /// <summary>
    /// Returns a dictionary of custom values to vary by.
    /// </summary>
    public Dictionary<string, string> VaryByCustom { get; } = new(StringComparer.OrdinalIgnoreCase);

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
