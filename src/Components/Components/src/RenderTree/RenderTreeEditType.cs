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
    //Describes the type of a render tree edit operation.
    public enum RenderTreeEditType: int
    {
        /// <summary>
        /// Indicates that a new frame should be inserted before the specified tree frame.
        /// </summary>
        PrependFrame = 1,

        /// <summary>
        /// Indicates that the specified tree frame should be removed.
        /// </summary>
        RemoveFrame = 2,

        /// <summary>
        /// Indicates that an attribute value should be applied to the specified frame.
        /// This may be a change to an existing attribute, or the addition of a new attribute.
        /// </summary>
        SetAttribute = 3,

        /// <summary>
        /// Indicates that a named attribute should be removed from the specified frame.
        /// </summary>
        RemoveAttribute = 4,

        /// <summary>
        /// Indicates that the text content of the specified frame (which must be a text frame)
        /// should be updated.
        /// </summary>
        UpdateText = 5,

        /// <summary>
        /// Indicates that the edit position should move inside the specified frame.
        /// </summary>
        StepIn = 6,

        /// <summary>
        /// Indicates that there are no further edit operations on the current frame, and the
        /// edit position should move back to the parent frame.
        /// </summary>
        StepOut = 7,

        /// <summary>
        /// Indicates that the markup content of the specified frame (which must be a markup frame)
        /// should be updated.
        /// </summary>
        UpdateMarkup = 8,

        /// <summary>
        /// An entry in a sparse permutation list. That is, a list of old indices with
        /// corresponding new indices, which altogether describe a valid permutation of
        /// the children at the current edit position.
        /// </summary>
        PermutationListEntry = 9,

        /// <summary>
        /// Indicates that the preceding series of <see cref="PermutationListEntry"/> entries
        /// is now complete.
        /// </summary>
        PermutationListEnd = 10,
    }
}
