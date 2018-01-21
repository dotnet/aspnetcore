// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Blazor.RenderTree
{
    internal class RenderTreeDiff
    {
        private const int MinBufferLength = 10;
        private RenderTreeDiffEntry[] _entries = new RenderTreeDiffEntry[10];
        private int _entriesInUse = 0;

        public ArraySegment<RenderTreeDiffEntry> ComputeDifference(
            ArraySegment<RenderTreeNode> oldTree,
            ArraySegment<RenderTreeNode> newTree)
        {
            _entriesInUse = 0;
            AppendDiffEntriesForRange(oldTree.Array, 0, oldTree.Count, newTree.Array, 0, newTree.Count);

            // If the previous usage of the buffer showed that we have allocated
            // much more space than needed, free up the excess memory
            var shrinkToLength = Math.Max(MinBufferLength, _entries.Length / 2);
            if (_entriesInUse < shrinkToLength)
            {
                Array.Resize(ref _entries, shrinkToLength);
            }

            return new ArraySegment<RenderTreeDiffEntry>(_entries, 0, _entriesInUse);
        }

        private void AppendDiffEntriesForRange(
            RenderTreeNode[] oldTree, int oldStartIndex, int oldEndIndexExcl,
            RenderTreeNode[] newTree, int newStartIndex, int newEndIndexExcl)
        {
            var hasMoreOld = oldEndIndexExcl > 0;
            var hasMoreNew = newEndIndexExcl > 0;
            var prevOldSeq = -1;
            var prevNewSeq = -1;
            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;

                if (oldSeq == newSeq)
                {
                    AppendDiffEntriesForNodesWithSameSequence(oldTree, oldStartIndex, newTree, newStartIndex);
                    oldStartIndex = NextSiblingIndex(oldTree, oldStartIndex);
                    newStartIndex = NextSiblingIndex(newTree, newStartIndex);
                    hasMoreOld = oldEndIndexExcl > oldStartIndex;
                    hasMoreNew = newEndIndexExcl > newStartIndex;
                    prevOldSeq = oldSeq;
                    prevNewSeq = newSeq;
                }
                else
                {
                    bool treatAsInsert;
                    var oldLoopedBack = oldSeq <= prevOldSeq;
                    var newLoopedBack = newSeq <= prevNewSeq;
                    if (oldLoopedBack == newLoopedBack)
                    {
                        // Both sequences are proceeding through the same loop block, so do a simple
                        // preordered merge join (picking from whichever side brings us closer to being
                        // back in sync)
                        treatAsInsert = newSeq < oldSeq;

                        if (oldLoopedBack)
                        {
                            // If both old and new have now looped back, we must reset their 'looped back'
                            // tracker so we can treat them as proceeding through the same loop block
                            prevOldSeq = prevNewSeq = -1;
                        }
                    }
                    else if (oldLoopedBack)
                    {
                        // Old sequence looped back but new one didn't
                        // The new sequence either has some extra trailing elements in the current loop block
                        // which we should insert, or omits some old trailing loop blocks which we should delete
                        // TODO: Find a way of not recomputing this next flag on every iteration
                        var newLoopsBackLater = false;
                        for (var testIndex = newStartIndex + 1; testIndex < newEndIndexExcl; testIndex++)
                        {
                            if (newTree[testIndex].Sequence < newSeq)
                            {
                                newLoopsBackLater = true;
                                break;
                            }
                        }

                        // If the new sequence loops back later to an earlier point than this,
                        // then we know it's part of the existing loop block (so should be inserted).
                        // If not, then it's unrelated to the previous loop block (so we should treat
                        // the old items as trailing loop blocks to be removed).
                        treatAsInsert = newLoopsBackLater;
                    }
                    else
                    {
                        // New sequence looped back but old one didn't
                        // The old sequence either has some extra trailing elements in the current loop block
                        // which we should delete, or the new sequence has extra trailing loop blocks which we
                        // should insert
                        // TODO: Find a way of not recomputing this next flag on every iteration
                        var oldLoopsBackLater = false;
                        for (var testIndex = oldStartIndex + 1; testIndex < oldEndIndexExcl; testIndex++)
                        {
                            if (oldTree[testIndex].Sequence < oldSeq)
                            {
                                oldLoopsBackLater = true;
                                break;
                            }
                        }

                        // If the old sequence loops back later to an earlier point than this,
                        // then we know it's part of the existing loop block (so should be removed).
                        // If not, then it's unrelated to the previous loop block (so we should treat
                        // the new items as trailing loop blocks to be inserted).
                        treatAsInsert = !oldLoopsBackLater;
                    }

                    if (treatAsInsert)
                    {
                        Append(RenderTreeDiffEntry.PrependNode(newStartIndex));
                        newStartIndex = NextSiblingIndex(newTree, newStartIndex);
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevNewSeq = newSeq;
                    }
                    else
                    {
                        Append(RenderTreeDiffEntry.RemoveNode());
                        oldStartIndex = NextSiblingIndex(oldTree, oldStartIndex);
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                        prevOldSeq = oldSeq;
                    }
                }
            }
        }

        private static int NextSiblingIndex(RenderTreeNode[] tree, int nodeIndex)
        {
            var descendantsEndIndex = tree[nodeIndex].ElementDescendantsEndIndex;
            return (descendantsEndIndex == 0 ? nodeIndex : descendantsEndIndex) + 1;
        }

        private void AppendDiffEntriesForNodesWithSameSequence(
            RenderTreeNode[] oldTree, int oldNodeIndex,
            RenderTreeNode[] newTree, int newNodeIndex)
        {
            // We can assume that the old and new nodes are of the same type, because they correspond
            // to the same sequence number (and if not, the behaviour is undefined).
            switch (newTree[newNodeIndex].NodeType)
            {
                case RenderTreeNodeType.Text:
                    {
                        var oldText = oldTree[oldNodeIndex].TextContent;
                        var newText = newTree[newNodeIndex].TextContent;
                        if (string.Equals(oldText, newText, StringComparison.Ordinal))
                        {
                            Append(RenderTreeDiffEntry.Continue());
                        }
                        else
                        {
                            Append(RenderTreeDiffEntry.UpdateText(newNodeIndex));
                        }
                        break;
                    }

                case RenderTreeNodeType.Element:
                    {
                        var oldElementName = oldTree[oldNodeIndex].ElementName;
                        var newElementName = newTree[newNodeIndex].ElementName;
                        if (string.Equals(oldElementName, newElementName, StringComparison.Ordinal))
                        {
                            // TODO: Compare attributes
                            // TODO: Then, recurse into children
                            Append(RenderTreeDiffEntry.Continue());
                        }
                        else
                        {
                            // Elements with different names are treated as completely unrelated
                            Append(RenderTreeDiffEntry.PrependNode(newNodeIndex));
                            Append(RenderTreeDiffEntry.RemoveNode());
                        }
                        break;
                    }

                case RenderTreeNodeType.Component:
                    {
                        var oldComponentType = oldTree[oldNodeIndex].ComponentType;
                        var newComponentType = newTree[newNodeIndex].ComponentType;
                        if (oldComponentType == newComponentType)
                        {
                            // TODO: Compare attributes and notify the existing child component
                            // instance of any changes
                            // TODO: Also copy the existing child component instance to the new
                            // tree so we can preserve it across multiple updates

                            Append(RenderTreeDiffEntry.Continue());
                        }
                        else
                        {
                            // Child components of different types are treated as completely unrelated
                            Append(RenderTreeDiffEntry.PrependNode(newNodeIndex));
                            Append(RenderTreeDiffEntry.RemoveNode());
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException($"Not yet implemented: diffing for nodes of type {newTree[newNodeIndex].NodeType}");
            }
        }

        private void Append(RenderTreeDiffEntry entry)
        {
            if (_entriesInUse == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            _entries[_entriesInUse++] = entry;
        }
    }
}
