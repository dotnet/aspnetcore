// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Contains context for a change to the browser's current location.
/// </summary>
public class LocationChangingContext
{
    private readonly CancellationTokenSource _cts;

    internal LocationChangingContext(string destinationUrl, bool isNavigationIntercepted, CancellationTokenSource cts)
    {
        TargetLocation = destinationUrl;
        IsNavigationIntercepted = isNavigationIntercepted;

        _cts = cts;
    }

    /// <summary>
    /// Gets the target location.
    /// </summary>
    public string TargetLocation { get; }

    /// <summary>
    /// Gets whether this navigation was intercepted from a link.
    /// </summary>
    public bool IsNavigationIntercepted { get; }

    /// <summary>
    /// Gets a <see cref="System.Threading.CancellationToken"/> that can be used to determine if this navigation was canceled.
    /// </summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// Prevents this navigation from continuing.
    /// </summary>
    public void PreventNavigation()
    {
        _cts.Cancel();
    }
}
