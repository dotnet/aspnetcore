// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Describes changes to a component's render tree between successive renders,
    /// as well as the resulting state.
    /// </summary>
    public readonly struct RenderTreeDiff
    {
        /// <summary>
        /// Gets the ID of the component.
        /// </summary>
        public int ComponentId { get; }

        /// <summary>
        /// Gets the changes to the render tree since a previous state.
        /// </summary>
        public ArrayRange<RenderTreeEdit> Edits { get; }

        /// <summary>
        /// Gets the latest render tree. That is, the result of applying the <see cref="Edits"/>
        /// to the previous state.
        /// </summary>
        public ArrayRange<RenderTreeFrame> CurrentState { get; }

        internal RenderTreeDiff(
            int componentId,
            ArrayRange<RenderTreeEdit> entries,
            ArrayRange<RenderTreeFrame> referenceTree)
        {
            ComponentId = componentId;
            Edits = entries;
            CurrentState = referenceTree;
        }
    }
}
