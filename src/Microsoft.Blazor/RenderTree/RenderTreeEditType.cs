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
        /// Indicates that there are no further operations on the current tree node, and
        /// so the edit position should move to the next sibling (or if there is no next
        /// sibling, then to the position where one would be).
        /// </summary>
        Continue = 1,

        /// <summary>
        /// Indicates that a new node should be inserted before the current tree node.
        /// </summary>
        PrependNode = 2,

        /// <summary>
        /// Indicates that the current tree node should be removed, and that the edit position
        /// should then move to the next sibling (or if there is no next sibling, then to the
        /// position where one would be).
        /// </summary>
        RemoveNode = 3,

        /// <summary>
        /// Indicates that an attribute value should be applied to the current node.
        /// This may be a change to an existing attribute, or the addition of a new attribute.
        /// </summary>
        SetAttribute = 4,

        /// <summary>
        /// Indicates that a named attribute should be removed from the current node.
        /// </summary>
        RemoveAttribute = 5,

        /// <summary>
        /// Indicates that the text content of the current node (which must be a text node)
        /// should be updated.
        /// </summary>
        UpdateText = 6,

        /// <summary>
        /// Indicates that the edit position should move inside the current node to its first
        /// child (or, if there are no children, to a point where a first child would be).
        /// </summary>
        StepIn = 7,

        /// <summary>
        /// Indicates that there are no further edit operations on the current node, and the
        /// edit position should move to the next sibling of the parent node (or if it does not
        /// have a next sibling, then to the position where one would be).
        /// </summary>
        StepOut = 8,
    }
}
