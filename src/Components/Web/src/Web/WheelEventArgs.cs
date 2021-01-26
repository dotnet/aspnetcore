// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Supplies information about a mouse wheel event that is being raised.
    /// </summary>
    public class WheelEventArgs : MouseEventArgs
    {
        /// <summary>
        /// The horizontal scroll amount.
        /// </summary>
        public double DeltaX { get; set; }

        /// <summary>
        /// The vertical scroll amount.
        /// </summary>
        public double DeltaY { get; set; }

        /// <summary>
        /// The scroll amount for the z-axis.
        /// </summary>
        public double DeltaZ { get; set; }

        /// <summary>
        /// The unit of the delta values scroll amount.
        /// </summary>
        public long DeltaMode { get; set; }
    }
}
