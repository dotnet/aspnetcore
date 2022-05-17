// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents vary-by rules.
/// </summary>
public class CachedVaryByRules
{
    /// <summary>
    /// Gets the custom values to vary by.
    /// </summary>
    public IDictionary<string, string> VaryByCustom { get; } = ImmutableDictionary<string, string>.Empty;

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
