// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Represents a single edit operation on a component's render tree.
    /// </summary>
    public struct RenderTreeEdit
    {
        /// <summary>
        /// Gets the type of the edit operation.
        /// </summary>
        public RenderTreeEditType Type { get; private set; }

        /// <summary>
        /// Gets the index of the sibling frame that the edit relates to.
        /// </summary>
        public int SiblingIndex { get; private set; }

        /// <summary>
        /// Gets the index of related data in an associated render tree. For example, if the
        /// <see cref="Type"/> value is <see cref="RenderTreeEditType.PrependFrame"/>, gets the
        /// index of the new frame data in an associated render tree.
        /// </summary>
        public int NewTreeIndex { get; private set; }

        /// <summary>
        /// If the <see cref="Type"/> value is <see cref="RenderTreeEditType.RemoveAttribute"/>,
        /// gets the name of the attribute that is being removed.
        /// </summary>
        public string RemovedAttributeName { get; private set; }

        internal static RenderTreeEdit RemoveFrame(int siblingIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.RemoveFrame,
            SiblingIndex = siblingIndex
        };

        internal static RenderTreeEdit PrependFrame(int siblingIndex, int newTreeIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.PrependFrame,
            SiblingIndex = siblingIndex,
            NewTreeIndex = newTreeIndex
        };

        internal static RenderTreeEdit UpdateText(int siblingIndex, int newTreeIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.UpdateText,
            SiblingIndex = siblingIndex,
            NewTreeIndex = newTreeIndex
        };

        internal static RenderTreeEdit SetAttribute(int siblingIndex, int newFrameIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.SetAttribute,
            SiblingIndex = siblingIndex,
            NewTreeIndex = newFrameIndex
        };

        internal static RenderTreeEdit RemoveAttribute(int siblingIndex, string name) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.RemoveAttribute,
            SiblingIndex = siblingIndex,
            RemovedAttributeName = name
        };

        internal static RenderTreeEdit StepIn(int siblingIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.StepIn,
            SiblingIndex = siblingIndex
        };

        internal static RenderTreeEdit StepOut() => new RenderTreeEdit
        {
            Type = RenderTreeEditType.StepOut
        };
    }
}
