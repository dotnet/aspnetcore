// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// <see cref="EventArgs" /> for <see cref="NavigationManager.NotFound" />.
/// </summary>
public class NotFoundEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundEventArgs" />.
    /// </summary>
    /// <param name="isNavigationIntercepted">A value that determines if navigation for the link was intercepted.</param>
    public NotFoundEventArgs(bool isNavigationIntercepted)
    {
        IsNavigationIntercepted = isNavigationIntercepted;
    }

    /// <summary>
    /// Gets a value that determines if navigation to NotFound page was intercepted.
    /// </summary>
    public bool IsNavigationIntercepted { get; }

    /// <summary>
    /// Gets the state associated with the current history entry.
    /// </summary>
    public string? HistoryEntryState { get; internal init; }
}
