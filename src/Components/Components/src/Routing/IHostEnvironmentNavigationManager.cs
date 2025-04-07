// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// An optional interface for <see cref="NavigationManager" /> implementations that must be initialized
/// by the host.
/// </summary>
public interface IHostEnvironmentNavigationManager
{
    /// <summary>
    /// Initializes the <see cref="NavigationManager" />.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="uri">The absolute URI.</param>
    void Initialize(string baseUri, string uri);

    /// <summary>
    /// An event that is triggered when SSR navigation occurs.
    /// </summary>
    /// <remarks>
    /// This event allows subscribers to respond to SSR navigation actions, such as updating state or performing side effects.
    /// </remarks>
    event EventHandler<NavigationEventArgs> OnNavigateTo;
}
