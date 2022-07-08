// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Contains context for a change to the browser's current location.
/// </summary>
public class LocationChangingContext
{
    internal LocationChangingContext(string location, bool isNavigationIntercepted, bool forceLoad, CancellationToken cancellationToken)
    {
        Location = location;
        IsNavigationIntercepted = isNavigationIntercepted;
        ForceLoad = forceLoad;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the URI being navigated to.
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Gets whether this navigation was intercepted from a link.
    /// </summary>
    public bool IsNavigationIntercepted { get; }

    /// <summary>
    /// Gets whether the navigation is attempting to bypass client-side routing.
    /// </summary>
    public bool ForceLoad { get; }

    /// <summary>
    /// Gets a <see cref="System.Threading.CancellationToken"/> that can be used to determine if this navigation gets canceled
    /// by a successive navigation.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets whether this navigation has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels this navigation.
    /// </summary>
    public void Cancel()
    {
        IsCanceled = true;
    }
}
