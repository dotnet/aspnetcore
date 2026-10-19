// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Describes which vary-by dimensions are active on an enclosing CacheView.
/// Used as flags in <see cref="CacheConditionAttribute"/> to express conditional
/// cache inclusion, and internally to communicate the active dimensions to the renderer.
/// </summary>
[Flags]
public enum CacheVaryBy
{
    /// <summary>
    /// No vary-by dimensions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Vary by query string parameters.
    /// </summary>
    Query = 1 << 0,

    /// <summary>
    /// Vary by route parameters.
    /// </summary>
    Route = 1 << 1,

    /// <summary>
    /// Vary by HTTP header values.
    /// </summary>
    Header = 1 << 2,

    /// <summary>
    /// Vary by cookie values.
    /// </summary>
    Cookie = 1 << 3,

    /// <summary>
    /// Vary by the authenticated user.
    /// </summary>
    User = 1 << 4,

    /// <summary>
    /// Vary by culture.
    /// </summary>
    Culture = 1 << 5,
}
