// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Blazor.RenderTree
{
    internal class RenderTreeDiffComputer
    {
        private const int MinBufferLength = 10;
        private RenderTreeEdit[] _entries = new RenderTreeEdit[10];
        private int _entriesInUse;

        public RenderTreeDiff ComputeDifference(
            ArraySegment<RenderTreeNode> oldTree,
            ArraySegment<RenderTreeNode> newTree)
        {
            _entriesInUse = 0;
            var siblingIndex = 0;
            AppendDiffEntriesForRange(oldTree.Array, 0, oldTree.Count, newTree.Array, 0, newTree.Count, ref siblingIndex);

            // If the previous usage of the buffer showed that we have allocated
            // much more space than needed, free up the excess memory
            var shrinkToLength = Math.Max(MinBufferLength, _entries.Length / 2);
            if (_entriesInUse < shrinkToLength)
            {
                Array.Resize(ref _entries, shrinkToLength);
            }

            return new RenderTreeDiff(
                new ArraySegment<RenderTreeEdit>(_entries, 0, _entriesInUse),
                newTree);
        }

        private void AppendDiffEntriesForRange(
            RenderTreeNode[] oldTree, int oldStartIndex, int oldEndIndexExcl,
            RenderTreeNode[] newTree, int newStartIndex, int newEndIndexExcl,
            ref int siblingIndex)
        {
            var hasMoreOld = oldEndIndexExcl > oldStartIndex;
            var hasMoreNew = newEndIndexExcl > newStartIndex;
            var prevOldSeq = -1;
            var prevNewSeq = -1;
            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;

                if (oldSeq == newSeq)
                {
                    AppendDiffEntriesForNodesWithSameSequence(oldTree, oldStartIndex, newTree, newStartIndex, ref siblingIndex);
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
                        if (newTree[newStartIndex].NodeType == RenderTreeNodeType.Attribute)
                        {
                            Append(RenderTreeEdit.SetAttribute(siblingIndex, newStartIndex));
                            newStartIndex++;
                        }
                        else
                        {
                            Append(RenderTreeEdit.PrependNode(siblingIndex, newStartIndex));
                            newStartIndex = NextSiblingIndex(newTree, newStartIndex);
                            siblingIndex++;
                        }

                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevNewSeq = newSeq;
                    }
                    else
                    {
                        if (oldTree[oldStartIndex].NodeType == RenderTreeNodeType.Attribute)
                        {
                            Append(RenderTreeEdit.RemoveAttribute(siblingIndex, oldTree[oldStartIndex].AttributeName));
                            oldStartIndex++;
                        }
                        else
                        {
                            Append(RenderTreeEdit.RemoveNode(siblingIndex));
                            oldStartIndex = NextSiblingIndex(oldTree, oldStartIndex);
                        }
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
            RenderTreeNode[] newTree, int newNodeIndex,
            ref int siblingIndex)
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
                            siblingIndex++;
                        }
                        else
                        {
                            Append(RenderTreeEdit.UpdateText(siblingIndex, newNodeIndex));
                        }
                        break;
                    }

                case RenderTreeNodeType.Element:
                    {
                        var oldElementName = oldTree[oldNodeIndex].ElementName;
                        var newElementName = newTree[newNodeIndex].ElementName;
                        if (string.Equals(oldElementName, newElementName, StringComparison.Ordinal))
                        {
                            var oldNodeAttributesEndIndexExcl = GetAttributesEndIndexExclusive(oldTree, oldNodeIndex);
                            var newNodeAttributesEndIndexExcl = GetAttributesEndIndexExclusive(newTree, newNodeIndex);

                            // Diff the attributes
                            AppendDiffEntriesForRange(
                                oldTree, oldNodeIndex + 1, oldNodeAttributesEndIndexExcl,
                                newTree, newNodeIndex + 1, newNodeAttributesEndIndexExcl,
                                ref siblingIndex);

                            // Diff the children
                            var oldNodeChildrenEndIndexExcl = oldTree[oldNodeIndex].ElementDescendantsEndIndex + 1;
                            var newNodeChildrenEndIndexExcl = newTree[newNodeIndex].ElementDescendantsEndIndex + 1;
                            var hasChildrenToProcess =
                                oldNodeChildrenEndIndexExcl > oldNodeAttributesEndIndexExcl ||
                                newNodeChildrenEndIndexExcl > newNodeAttributesEndIndexExcl;
                            if (hasChildrenToProcess)
                            {
                                Append(RenderTreeEdit.StepIn(siblingIndex));
                                var childSiblingIndex = 0;
                                AppendDiffEntriesForRange(
                                    oldTree, oldNodeAttributesEndIndexExcl, oldNodeChildrenEndIndexExcl,
                                    newTree, newNodeAttributesEndIndexExcl, newNodeChildrenEndIndexExcl,
                                    ref childSiblingIndex);
                                Append(RenderTreeEdit.StepOut());
                                siblingIndex++;
                            }
                            else
                            {
                                siblingIndex++;
                            }
                        }
                        else
                        {
                            // Elements with different names are treated as completely unrelated
                            Append(RenderTreeEdit.PrependNode(siblingIndex, newNodeIndex));
                            siblingIndex++;
                            Append(RenderTreeEdit.RemoveNode(siblingIndex));
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
                            siblingIndex++;
                        }
                        else
                        {
                            // Child components of different types are treated as completely unrelated
                            Append(RenderTreeEdit.PrependNode(siblingIndex, newNodeIndex));
                            siblingIndex++;
                            Append(RenderTreeEdit.RemoveNode(siblingIndex));
                        }
                        break;
                    }

                case RenderTreeNodeType.Attribute:
                    {
                        var oldName = oldTree[oldNodeIndex].AttributeName;
                        var newName = newTree[newNodeIndex].AttributeName;
                        if (string.Equals(oldName, newName, StringComparison.Ordinal))
                        {
                            var changed =
                                !string.Equals(oldTree[oldNodeIndex].AttributeValue, newTree[newNodeIndex].AttributeValue, StringComparison.Ordinal)
                                || oldTree[oldNodeIndex].AttributeEventHandlerValue != newTree[newNodeIndex].AttributeEventHandlerValue;
                            if (changed)
                            {
                                Append(RenderTreeEdit.SetAttribute(siblingIndex, newNodeIndex));
                            }
                        }
                        else
                        {
                            // Since this code path is never reachable for Razor components (because you
                            // can't have two different attribute names from the same source sequence), we
                            // could consider removing the 'name equality' check entirely for perf
                            Append(RenderTreeEdit.SetAttribute(siblingIndex, newNodeIndex));
                            Append(RenderTreeEdit.RemoveAttribute(siblingIndex, oldName));
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException($"Encountered unsupported node type during diffing: {newTree[newNodeIndex].NodeType}");
            }
        }

        private int GetAttributesEndIndexExclusive(RenderTreeNode[] tree, int rootIndex)
        {
            var descendantsEndIndex = tree[rootIndex].ElementDescendantsEndIndex;
            var index = rootIndex + 1;
            for (; index <= descendantsEndIndex; index++)
            {
                if (tree[index].NodeType != RenderTreeNodeType.Attribute)
                {
                    break;
                }
            }

            return index;
        }

        private void Append(RenderTreeEdit entry)
        {
            if (entry.Type == RenderTreeEditType.StepOut)
            {
                // If the preceding node is a StepIn, then the StepOut cancels it out
                if (_entriesInUse > 0 && _entries[_entriesInUse - 1].Type == RenderTreeEditType.StepIn)
                {
                    _entriesInUse--;
                    return;
                }
            }

            if (_entriesInUse == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            _entries[_entriesInUse++] = entry;
        }
    }
}
