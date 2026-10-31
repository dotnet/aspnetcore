// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// Contract to setup scroll to location hash.
/// </summary>
public interface IScrollToLocationHash
{
    /// <summary>
    /// Refreshes scroll position for hash on the client.
    /// </summary>
    /// <param name="locationAbsolute">Absolute URL of location</param> 
    /// <returns>A <see cref="Task" /> that represents the asynchronous operation.</returns>
    Task RefreshScrollPositionForHash(string locationAbsolute);
}
