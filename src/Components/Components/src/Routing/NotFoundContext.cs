// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Contains context for a change to the browser's current location.
/// </summary>
public sealed class NotFoundContext
{
    internal bool DidPreventRendering { get; private set; }

    /// <summary>
    /// Gets a <see cref="System.Threading.CancellationToken"/> that can be used to determine if this navigation was canceled
    /// (for example, because the user has triggered a different navigation).
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Prevents this navigation from continuing.
    /// </summary>
    public void PreventRendering()
    {
        DidPreventRendering = true;
    }

}
