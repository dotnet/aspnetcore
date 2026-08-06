// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>
/// Describes why a virtualization spacer became visible, as reported by the JavaScript
/// <c>IntersectionObserver</c>. The component uses this to decide whether a spacer callback should
/// load data, instead of inferring user-vs-programmatic scrolling from timing.
/// </summary>
/// <remarks>
/// The numeric values must stay in sync with the <c>SpacerVisibilityReason</c> constant in
/// <c>Virtualize.ts</c>.
/// </remarks>
internal enum SpacerVisibilityReason
{
    /// <summary>The user scrolled the spacer into view.</summary>
    UserScroll = 0,

    /// <summary>
    /// A scroll the component itself performed (initial positioning, <c>ScrollToItemAsync</c>, or an
    /// anchor restore) transiently exposed the spacer. Such callbacks are ignored — acting on them would
    /// undo the programmatic position.
    /// </summary>
    ProgrammaticScroll = 1,

    /// <summary>
    /// The spacer is visible at rest because the rendered window does not cover the viewport, so the
    /// window should grow toward the spacer to fill it.
    /// </summary>
    ViewportFill = 2,
}
