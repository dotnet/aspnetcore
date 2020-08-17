// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace Microsoft.AspNetCore.Components.Web.Virtualization
{
    /// <summary>
    /// Contains context for a placeholder in a virtualized list.
    /// </summary>
    public readonly struct PlaceholderContext
    {
        /// <summary>
        /// The item index of the placeholder.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The size of the placeholder in pixels.
        /// <para>
        /// For virtualized components with vertical scrolling, this would be the height of the placeholder in pixels.
        /// For virtualized components with horizontal scrolling, this would be the width of the placeholder in pixels.
        /// </para>
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// Constructs a new <see cref="PlaceholderContext"/> instance.
        /// </summary>
        /// <param name="index">The item index of the placeholder.</param>
        /// <param name="size">The size of the placeholder in pixels.</param>
        public PlaceholderContext(int index, float size = 0f)
        {
            Index = index;
            Size = size;
        }
    }
}
