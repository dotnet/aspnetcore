// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Describes an event involving a spacer in a virtualized list.
    /// </summary>
    public class SpacerEventArgs : EventArgs
    {
        /// <summary>
        /// The new size of the spacer.
        /// </summary>
        public float SpacerSize { get; }

        /// <summary>
        /// The current size of the spacer's container.
        /// </summary>
        public float ContainerSize { get; }

        /// <summary>
        /// Instantiates a new <see cref="SpacerEventArgs"/> instance.
        /// </summary>
        /// <param name="spacerSize">The new size of the spacer.</param>
        /// <param name="containerSize">The current size of the spacer's container.</param>
        public SpacerEventArgs(float spacerSize, float containerSize)
        {
            SpacerSize = spacerSize;
            ContainerSize = containerSize;
        }
    }
}
