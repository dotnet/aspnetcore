// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// <see cref="EventArgs" /> for <see cref="NavigationManager.LocationChanged" />.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the URI of the navigation event.
    /// </summary>
    public string Uri { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="NavigationEventArgs"/>.
    /// </summary>
    /// <param name="uri">The URI of the navigation event.</param>
    public NavigationEventArgs(string uri)
    {
        Uri = uri;
    }
}