// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

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
    /// anchor restore) transiently exposed the spacer.
    /// </summary>
    ProgrammaticScroll = 1,

    /// <summary>
    /// The spacer is visible at rest because the rendered window does not cover the viewport, so the
    /// window should grow toward the spacer to fill it.
    /// </summary>
    ViewportFill = 2,

    /// <summary>
    /// Not a real spacer-visibility event. Refines the item-size estimate from the already-rendered
    /// window without triggering window redistribution.
    /// </summary>
    RenderedContentMeasurement = 3,
}
