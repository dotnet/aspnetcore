// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.RenderTree
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
        /// Gets the index of related data in an associated render tree. For example, if the
        /// <see cref="Type"/> value is <see cref="RenderTreeEditType.PrependNode"/>, gets the
        /// index of the new node data in an associated render tree.
        /// </summary>
        public int NewTreeIndex { get; private set; }

        /// <summary>
        /// If the <see cref="Type"/> value is <see cref="RenderTreeEditType.RemoveAttribute"/>,
        /// gets the name of the attribute that is being removed.
        /// </summary>
        public string RemovedAttributeName { get; private set; }

        internal static RenderTreeEdit Continue() => new RenderTreeEdit
        {
            Type = RenderTreeEditType.Continue
        };

        internal static RenderTreeEdit RemoveNode() => new RenderTreeEdit
        {
            Type = RenderTreeEditType.RemoveNode
        };

        internal static RenderTreeEdit PrependNode(int newTreeIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.PrependNode,
            NewTreeIndex = newTreeIndex
        };

        internal static RenderTreeEdit UpdateText(int newTreeIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.UpdateText,
            NewTreeIndex = newTreeIndex
        };

        internal static RenderTreeEdit SetAttribute(int newNodeIndex) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.SetAttribute,
            NewTreeIndex = newNodeIndex
        };

        internal static RenderTreeEdit RemoveAttribute(string name) => new RenderTreeEdit
        {
            Type = RenderTreeEditType.RemoveAttribute,
            RemovedAttributeName = name
        };

        internal static RenderTreeEdit StepIn() => new RenderTreeEdit
        {
            Type = RenderTreeEditType.StepIn
        };

        internal static RenderTreeEdit StepOut() => new RenderTreeEdit
        {
            Type = RenderTreeEditType.StepOut
        };
    }
}
