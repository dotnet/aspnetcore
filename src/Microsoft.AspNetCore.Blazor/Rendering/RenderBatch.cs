// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    /// <summary>
    /// Describes a set of UI changes.
    /// </summary>
    public readonly struct RenderBatch
    {
        /// <summary>
        /// Gets the changes to components that were added or updated.
        /// </summary>
        public ArrayRange<RenderTreeDiff> UpdatedComponents { get; }

        internal RenderBatch(ArrayRange<RenderTreeDiff> updatedComponents)
        {
            UpdatedComponents = updatedComponents;
        }
    }
}
