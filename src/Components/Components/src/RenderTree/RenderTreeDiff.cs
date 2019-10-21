// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if IGNITOR
namespace Ignitor
#else
namespace Microsoft.AspNetCore.Components.RenderTree
#endif
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.RenderTree are not recommended for use outside
    /// of the Blazor framework. These types will change in future release.
    /// </summary>
    //
    // Describes changes to a component's render tree between successive renders.
    public readonly struct RenderTreeDiff
    {
        /// <summary>
        /// Gets the ID of the component.
        /// </summary>
        public readonly int ComponentId;

        /// <summary>
        /// Gets the changes to the render tree since a previous state.
        /// </summary>
        public readonly ArrayBuilderSegment<RenderTreeEdit> Edits;

        internal RenderTreeDiff(
            int componentId,
            ArrayBuilderSegment<RenderTreeEdit> entries)
        {
            ComponentId = componentId;
            Edits = entries;
        }
    }
}
