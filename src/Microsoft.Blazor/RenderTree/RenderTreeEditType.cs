// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.RenderTree
{
    /// <summary>
    /// Describes the type of a render tree edit operation.
    /// </summary>
    public enum RenderTreeEditType: int
    {
        /// <summary>
        /// Indicates that a new node should be inserted before the specified tree node.
        /// </summary>
        PrependNode = 1,

        /// <summary>
        /// Indicates that the specified tree node should be removed.
        /// </summary>
        RemoveNode = 2,

        /// <summary>
        /// Indicates that an attribute value should be applied to the specified node.
        /// This may be a change to an existing attribute, or the addition of a new attribute.
        /// </summary>
        SetAttribute = 3,

        /// <summary>
        /// Indicates that a named attribute should be removed from the specified node.
        /// </summary>
        RemoveAttribute = 4,

        /// <summary>
        /// Indicates that the text content of the specified node (which must be a text node)
        /// should be updated.
        /// </summary>
        UpdateText = 5,

        /// <summary>
        /// Indicates that the edit position should move inside the specified node.
        /// </summary>
        StepIn = 6,

        /// <summary>
        /// Indicates that there are no further edit operations on the current node, and the
        /// edit position should move back to the parent node.
        /// </summary>
        StepOut = 7,
    }
}
