// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Describes the type of a <see cref="RenderTreeFrame"/>.
    /// </summary>
    public enum RenderTreeFrameType: int
    {
        /// <summary>
        /// Represents a container for other frames.
        /// </summary>
        Element = 1,

        /// <summary>
        /// Represents text content.
        /// </summary>
        Text = 2,

        /// <summary>
        /// Represents a key-value pair associated with another <see cref="RenderTreeFrame"/>.
        /// </summary>
        Attribute = 3,

        /// <summary>
        /// Represents a child component.
        /// </summary>
        Component = 4,
    }
}
