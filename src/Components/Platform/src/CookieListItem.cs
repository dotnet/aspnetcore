// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a cookie returned by the Cookie Store API.
/// </summary>
public sealed class CookieListItem
{
    /// <summary>
    /// Gets the name of the cookie.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the value of the cookie.
    /// </summary>
    public string? Value { get; init; }
}
