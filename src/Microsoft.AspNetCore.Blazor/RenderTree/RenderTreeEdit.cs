// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Represents a single edit operation on a component's render tree.
    /// </summary>
    public readonly struct RenderTreeEdit
    {
        /// <summary>
        /// Gets the type of the edit operation.
        /// </summary>
        public readonly RenderTreeEditType Type;

        /// <summary>
        /// Gets the index of the sibling frame that the edit relates to.
        /// </summary>
        public readonly int SiblingIndex;

        /// <summary>
        /// Gets the index of related data in an associated render tree. For example, if the
        /// <see cref="Type"/> value is <see cref="RenderTreeEditType.PrependFrame"/>, gets the
        /// index of the new frame data in an associated render tree.
        /// </summary>
        public readonly int NewTreeIndex;

        /// <summary>
        /// If the <see cref="Type"/> value is <see cref="RenderTreeEditType.RemoveAttribute"/>,
        /// gets the name of the attribute that is being removed.
        /// </summary>
        public readonly string RemovedAttributeName;

        private RenderTreeEdit(RenderTreeEditType type) : this()
        {
            Type = type;
        }

        private RenderTreeEdit(RenderTreeEditType type, int siblingIndex) : this()
        {
            Type = type;
            SiblingIndex = siblingIndex;
        }

        private RenderTreeEdit(RenderTreeEditType type, int siblingIndex, int newTreeIndex) : this()
        {
            Type = type;
            SiblingIndex = siblingIndex;
            NewTreeIndex = newTreeIndex;
        }

        private RenderTreeEdit(RenderTreeEditType type, int siblingIndex, string removedAttributeName) : this()
        {
            Type = type;
            SiblingIndex = siblingIndex;
            RemovedAttributeName = removedAttributeName;
        }

        internal static RenderTreeEdit RemoveFrame(int siblingIndex)
            => new RenderTreeEdit(RenderTreeEditType.RemoveFrame, siblingIndex);

        internal static RenderTreeEdit PrependFrame(int siblingIndex, int newTreeIndex)
            => new RenderTreeEdit(RenderTreeEditType.PrependFrame, siblingIndex, newTreeIndex);

        internal static RenderTreeEdit UpdateText(int siblingIndex, int newTreeIndex)
            => new RenderTreeEdit(RenderTreeEditType.UpdateText, siblingIndex, newTreeIndex);

        internal static RenderTreeEdit SetAttribute(int siblingIndex, int newTreeIndex)
            => new RenderTreeEdit(RenderTreeEditType.SetAttribute, siblingIndex, newTreeIndex);

        internal static RenderTreeEdit RemoveAttribute(int siblingIndex, string name)
            => new RenderTreeEdit(RenderTreeEditType.RemoveAttribute, siblingIndex, name);

        internal static RenderTreeEdit StepIn(int siblingIndex)
            => new RenderTreeEdit(RenderTreeEditType.StepIn, siblingIndex);

        internal static RenderTreeEdit StepOut()
            => new RenderTreeEdit(RenderTreeEditType.StepOut);
    }
}
