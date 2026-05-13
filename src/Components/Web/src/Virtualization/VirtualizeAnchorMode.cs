// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>
/// Controls how the viewport behaves at the edges of the list when
/// new items arrive. Flags can be combined to pin both edges.
/// </summary>
[Flags]
public enum VirtualizeAnchorMode
{
    /// <summary>
    /// No edge pinning. The viewport stays at its current scroll position
    /// regardless of item changes.
    /// </summary>
    None = 0,

    /// <summary>
    /// Pins the viewport to the beginning of the list. When the user is
    /// at or near the top and new items arrive at the beginning, the viewport
    /// stays at the top showing the newest items — matching standard news
    /// feed / notification list UX.
    /// </summary>
    Beginning = 1,

    /// <summary>
    /// Pins the viewport to the end of the list. When the user is at or near
    /// the bottom and new items arrive at the end, the viewport auto-scrolls
    /// to show them. If the user has scrolled away, auto-scroll disengages
    /// until they return to the bottom — matching standard chat / log UX.
    /// </summary>
    End = 2,
}
