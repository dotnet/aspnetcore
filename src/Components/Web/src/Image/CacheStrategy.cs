// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// Defines the caching strategy for an image.
/// </summary>
public enum CacheStrategy
{
    /// <summary>
    /// No caching.
    /// </summary>
    None,

    /// <summary>
    /// Cache in memory.
    /// </summary>
    Memory
}
