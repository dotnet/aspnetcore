// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

/// <summary>
/// <see cref="EventArgs" /> for <see cref="NavigationManager.OnNotFound" />.
/// </summary>
public sealed class NotFoundEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path of NotFoundPage. If the path is set, it indicates that a subscriber has handled the rendering of the NotFound contents.
    /// </summary>
    public string? Path { get; set; }
}
