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
}
