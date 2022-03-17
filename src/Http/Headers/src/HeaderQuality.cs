// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Provides HTTP header quality factors.
/// </summary>
public static class HeaderQuality
{
    /// <summary>
    /// Quality factor to indicate a perfect match.
    /// </summary>
    public const double Match = 1.0;

    /// <summary>
    /// Quality factor to indicate no match.
    /// </summary>
    public const double NoMatch = 0.0;
}
