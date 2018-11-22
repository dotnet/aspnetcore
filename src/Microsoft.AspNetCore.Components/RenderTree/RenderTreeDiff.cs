// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Describes changes to a component's render tree between successive renders.
    /// </summary>
    public readonly struct RenderTreeDiff
    {
        /// <summary>
        /// Gets the ID of the component.
        /// </summary>
        public readonly int ComponentId;

        /// <summary>
        /// Gets the changes to the render tree since a previous state.
        /// </summary>
        public readonly ArraySegment<RenderTreeEdit> Edits;

        internal RenderTreeDiff(
            int componentId,
            ArraySegment<RenderTreeEdit> entries)
        {
            ComponentId = componentId;
            Edits = entries;
        }
    }
}
