// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.RenderTree
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
        public readonly ArrayRange<RenderTreeEdit> Edits;

        /// <summary>
        /// Gets render frames that may be referenced by entries in <see cref="Edits"/>.
        /// For example, edit entries of type <see cref="RenderTreeEditType.PrependFrame"/>
        /// will point to an entry in this array to specify the subtree to be prepended.
        /// </summary>
        public readonly ArrayRange<RenderTreeFrame> ReferenceFrames;

        internal RenderTreeDiff(
            int componentId,
            ArrayRange<RenderTreeEdit> entries,
            ArrayRange<RenderTreeFrame> referenceFrames)
        {
            ComponentId = componentId;
            Edits = entries;
            ReferenceFrames = referenceFrames;
        }
    }
}
