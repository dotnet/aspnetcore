// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.RenderTree
{
    internal struct RenderTreeDiffEntry
    {
        public RenderTreeDiffEntryType Type { get; private set; }
        public int NewTreeIndex { get; private set; }

        public static RenderTreeDiffEntry Continue() => new RenderTreeDiffEntry
        {
            Type = RenderTreeDiffEntryType.Continue
        };

        internal static RenderTreeDiffEntry RemoveNode() => new RenderTreeDiffEntry
        {
            Type = RenderTreeDiffEntryType.RemoveNode
        };

        internal static RenderTreeDiffEntry PrependNode(int newTreeIndex) => new RenderTreeDiffEntry
        {
            Type = RenderTreeDiffEntryType.PrependNode,
            NewTreeIndex = newTreeIndex
        };

        internal static RenderTreeDiffEntry UpdateText(int newTreeIndex) => new RenderTreeDiffEntry
        {
            Type = RenderTreeDiffEntryType.UpdateText,
            NewTreeIndex = newTreeIndex
        };
    }
}
