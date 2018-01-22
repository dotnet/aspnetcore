// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Blazor.RenderTree
{
    /// <summary>
    /// Describes changes to a component's render tree between successive renders.
    /// </summary>
    public struct RenderTreeDiff
    {
        /// <summary>
        /// Describes the render tree changes as a sequence of edit operations.
        /// </summary>
        public RenderTreeEdit[] Entries { get; private set; }

        /// <summary>
        /// An array of <see cref="RenderTreeNode"/> structures that may be referred to
        /// by entries in the <see cref="Entries"/> property.
        /// </summary>
        public ArraySegment<RenderTreeNode> ReferenceTree { get; private set; }
    }
}
