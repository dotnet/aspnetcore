// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Blazor.RenderTree
{
    /// <summary>
    /// Describes changes to a component's render tree between successive renders,
    /// as well as the resulting state.
    /// </summary>
    public readonly struct RenderTreeDiff
    {
        /// <summary>
        /// Gets the changes to the render tree since a previous state.
        /// </summary>
        public ArrayRange<RenderTreeEdit> Edits { get; }

        /// <summary>
        /// Gets the latest render tree. That is, the result of applying the <see cref="Edits"/>
        /// to the previous state.
        /// </summary>
        public ArrayRange<RenderTreeNode> CurrentState { get; }

        internal RenderTreeDiff(
            ArrayRange<RenderTreeEdit> entries,
            ArrayRange<RenderTreeNode> referenceTree)
        {
            Edits = entries;
            CurrentState = referenceTree;
        }
    }
}
