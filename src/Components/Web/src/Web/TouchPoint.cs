// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Represents a single contact point on a touch-sensitive device.
    /// The contact point is commonly a finger or stylus and the device may be a touchscreen or trackpad.
    /// </summary>
    public class TouchPoint
    {
        /// <summary>
        /// A unique identifier for this Touch object.
        /// A given touch point (say, by a finger) will have the same identifier for the duration of its movement around the surface.
        /// This lets you ensure that you're tracking the same touch all the time.
        /// </summary>
        public long Identifier { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the screen.
        /// </summary>
        public double ScreenX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top edge of the screen.
        /// </summary>
        public double ScreenY { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the browser viewport, not including any scroll offset.
        /// </summary>
        public double ClientX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top edge of the browser viewport, not including any scroll offset.
        /// </summary>
        public double ClientY { get; set; }

        /// <summary>
        /// The X coordinate of the touch point relative to the left edge of the document.
        /// Unlike <see cref="ClientX"/>, this value includes the horizontal scroll offset, if any.
        /// </summary>
        public double PageX { get; set; }

        /// <summary>
        /// The Y coordinate of the touch point relative to the top of the document.
        /// Unlike <see cref="ClientY"/>, this value includes the vertical scroll offset, if any.
        /// </summary>
        public double PageY { get; set; }
    }
}
