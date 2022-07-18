// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Contains context for a change to the browser's current location.
/// </summary>
public class LocationChangingContext
{
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// Constructs a new <see cref="LocationChangingContext"/> instance.
    /// </summary>
    /// <param name="targetLocation">The new location if the navigation continues.</param>
    /// <param name="isNavigationIntercepted">Whether this navigation was intercepted from a link.</param>
    /// <param name="cts">A <see cref="CancellationTokenSource"/> whose token can be used to determine if this navigation gets canceled.</param>
    public LocationChangingContext(string targetLocation, bool isNavigationIntercepted, CancellationTokenSource cts)
    {
        TargetLocation = targetLocation;
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
    /// Gets a <see cref="System.Threading.CancellationToken"/> that can be used to determine if this navigation was canceled
    /// (for example, because the user has triggered a different navigation).
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
